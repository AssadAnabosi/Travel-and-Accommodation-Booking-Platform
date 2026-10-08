using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommandHandler(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only update rooms for your own hotel.");

        room.Update(request.AdultCapacity, request.ChildCapacity);

        roomRepository.Update(room);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RoomDto(room.Id, room.HotelId, room.Number, room.RoomType.ToString(),
            room.AdultCapacity, room.ChildCapacity, room.BasePrice.Amount, room.BasePrice.Currency, room.IsActive,
            room.CreatedAt, room.ModifiedAt);
    }
}