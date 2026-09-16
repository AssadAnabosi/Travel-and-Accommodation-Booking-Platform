using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Bookings.Commands.CheckOutBooking;

public class CheckOutBookingCommandHandler(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CheckOutBookingCommand>
{
    public async Task Handle(CheckOutBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (!currentUserService.IsInRole("Admin") && booking.Room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only check out bookings for your own hotel.");

        booking.CheckOut();

        bookingRepository.Update(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}