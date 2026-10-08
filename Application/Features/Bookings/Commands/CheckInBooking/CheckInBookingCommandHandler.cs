using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Bookings.Commands.CheckInBooking;

public class CheckInBookingCommandHandler(
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CheckInBookingCommand>
{
    public async Task Handle(CheckInBookingCommand request, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdWithDetailsAsync(request.BookingId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Booking), request.BookingId);

        if (!currentUserService.IsInRole("Admin") && booking.Room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only check in bookings for your own hotel.");

        booking.CheckIn();

        bookingRepository.Update(booking);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}