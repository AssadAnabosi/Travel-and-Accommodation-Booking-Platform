using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomImages;

public class GetRoomImagesQueryHandler(IRoomRepository roomRepository, ICurrentUserService currentUserService)
    : IRequestHandler<GetRoomImagesQuery, IReadOnlyList<ImageDto>>
{
    public async Task<IReadOnlyList<ImageDto>> Handle(GetRoomImagesQuery request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithImagesTrackedAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view images for your own hotel's rooms.");

        return room.Images
            .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, i.Url, i.DisplayOrder))
            .ToList();
    }
}
