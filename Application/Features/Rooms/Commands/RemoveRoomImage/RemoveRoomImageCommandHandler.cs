using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.RemoveRoomImage;

public class RemoveRoomImageCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<RemoveRoomImageCommand>
{
    public async Task Handle(RemoveRoomImageCommand request, CancellationToken cancellationToken)
    {
        // Must be a tracked load, or the removal is never persisted (see decision #71 for hotel images).
        var room = await roomRepository.GetByIdWithImagesTrackedAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage images for your own hotel's rooms.");

        room.RemoveImage(request.ImageId); // ImageNotFoundException → 404 if it isn't this room's image

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
