using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Bookings.Queries.GetBookingConfirmationPdf;

public class GetBookingConfirmationPdfQueryHandler(
    IBookingRepository bookingRepository,
    ICurrentUserService currentUserService,
    IPdfGenerator pdfGenerator)
    : IRequestHandler<GetBookingConfirmationPdfQuery, byte[]>
{
    public async Task<byte[]> Handle(GetBookingConfirmationPdfQuery request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        var isOwnerOfBooking = booking.UserId == currentUserService.UserId;
        var isAdmin = currentUserService.IsInRole("Admin");
        var isHotelOwner = booking.Room.Hotel.OwnerId == currentUserService.UserId;

        if (!isOwnerOfBooking && !isAdmin && !isHotelOwner)
            throw new ForbiddenAccessException("You do not have access to this booking.");

        if (booking.Status == BookingStatus.Pending)
            throw new ConflictException("This booking has not been confirmed yet.");

        return pdfGenerator.GenerateBookingConfirmation(booking);
    }
}