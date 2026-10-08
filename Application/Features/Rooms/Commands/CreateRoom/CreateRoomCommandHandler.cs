using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Common;
using Domain.Entities;
using Domain.ValueObjects;
using MediatR;

namespace Application.Features.Rooms.Commands.CreateRoom;

public class CreateRoomCommandHandler(
    IHotelRepository hotelRepository,
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateRoomCommand, RoomDto>
{
    public async Task<RoomDto> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only add rooms to your own hotel.");

        var price = Money.Of(request.BasePrice, request.Currency);
        var room = Room.Create(hotel.Id, request.Number, request.RoomType, request.AdultCapacity, request.ChildCapacity,
            price);

        await roomRepository.AddAsync(room, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RoomDto(room.Id, room.HotelId, room.Number, room.RoomType.ToString(),
            room.AdultCapacity, room.ChildCapacity, room.BasePrice.Amount, room.BasePrice.Currency, room.IsActive,
            room.CreatedAt, room.ModifiedAt);
    }
}