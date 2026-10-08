using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.AddRoomImage;

public class AddRoomImageCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddRoomImageCommand, int>
{
    public async Task<int> Handle(AddRoomImageCommand request, CancellationToken cancellationToken)
    {
        // Tracked load: change tracking inserts the new image; no Update() of the whole graph needed.
        var room = await roomRepository.GetByIdWithImagesTrackedAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only manage images for your own hotel's rooms.");

        var image = room.AddImage(request.Url);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return image.Id;
    }
}
