using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.DeleteRoom;

public class DeleteRoomCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteRoomCommand>
{
    public async Task Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only delete rooms for your own hotel.");

        var hasHistory = await roomRepository.HasAnyBookingsAsync(room.Id, cancellationToken);

        if (hasHistory)
        {
            room.MarkDeleted();
            roomRepository.Update(room);
        }
        else
        {
            roomRepository.Remove(room);
        }

        roomRepository.Remove(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}