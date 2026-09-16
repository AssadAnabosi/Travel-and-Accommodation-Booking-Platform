using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Bookings.Common;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommandHandler(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IPaymentGateway paymentGateway,
    IEmailService emailService,
    IPdfGenerator pdfGenerator)
    : IRequestHandler<ConfirmBookingCommand, BookingDto>
{
    public async Task<BookingDto> Handle(ConfirmBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (booking.UserId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only confirm your own booking.");

        if (booking.Status != BookingStatus.Pending)
            throw new ConflictException($"Booking is not awaiting payment (current status: {booking.Status}).");
        
        var paymentResult = await paymentGateway.ChargeAsync(
            new PaymentRequest(booking.Id, booking.TotalPrice.Amount, booking.TotalPrice.Currency, request.CardToken),
            cancellationToken);

        if (!paymentResult.Success)
            throw new PaymentFailedException(paymentResult.FailureReason ?? "The payment could not be processed.");

        booking.Confirm();

        bookingRepository.Update(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var pdf = pdfGenerator.GenerateBookingConfirmation(booking);
        var emailBody =
            $"<p>Your booking at {booking.Room.Hotel.Name} is confirmed. Confirmation number: {booking.ConfirmationNumber}.</p>";

        await emailService.SendAsync(
            new EmailMessage(booking.User.Email, "Your booking is confirmed", emailBody, pdf,
                "booking-confirmation.pdf"),
            cancellationToken);

        return new BookingDto(booking.Id, booking.ConfirmationNumber, booking.Status.ToString(),
            booking.TotalPrice.Amount, booking.TotalPrice.Currency, booking.StayRange.StartDate,
            booking.StayRange.EndDate);
    }
}