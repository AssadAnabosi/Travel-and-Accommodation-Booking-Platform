using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler(
    IRoomRepository roomRepository,
    IBookingRepository bookingRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateBookingCommand, CreateBookingResponse>
{
    public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
                     ?? throw new ForbiddenAccessException("You must be logged in to book a room.");

        var room = await roomRepository.GetByIdForBookingAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        var range = DateRange.Of(request.CheckIn, request.CheckOut);

        // Fast, DB-level check first — cheaper than loading the full aggregate for the common "unavailable" case.
        var isAvailable = await roomRepository.IsAvailableAsync(room.Id, range, cancellationToken);
        if (!isAvailable)
            throw new RoomNotAvailableException(room.Id, range);

        var nightlyPrice = room.GetActivePrice(request.CheckIn);
        var totalPrice = Money.Of(nightlyPrice.Amount * range.Nights, nightlyPrice.Currency);

        var booking = Booking.Create(
            userId,
            room.Id,
            range,
            request.Adults,
            request.Children,
            totalPrice,
            request.SpecialRequests);

        // Re-enforced inside the aggregate — closes the gap between the check above and this write.
        room.Reserve(range, booking.Id);

        await bookingRepository.AddAsync(booking, cancellationToken);
        roomRepository.Update(room);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateBookingResponse(
            booking.Id,
            booking.ConfirmationNumber,
            booking.TotalPrice.Amount,
            booking.TotalPrice.Currency,
            booking.Status.ToString());
    }
}