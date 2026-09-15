using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.DeleteRoom;

public class DeleteRoomCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<DeleteRoomCommand>
{
    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only delete rooms for your own hotel.");

        if (await roomRepository.HasFutureBookingsAsync(room.Id, dateTimeProvider.Today, cancellationToken))
            throw new ConflictException("Cannot delete a room with current or upcoming bookings.");

        roomRepository.Remove(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}