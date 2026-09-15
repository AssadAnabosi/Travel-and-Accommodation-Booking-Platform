using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomById;

public class GetRoomByIdQueryHandler : IRequestHandler<GetRoomByIdQuery, RoomDto>
{
    private readonly IRoomRepository _roomRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRoomByIdQueryHandler(IRoomRepository roomRepository, ICurrentUserService currentUserService)
    {
        _roomRepository = roomRepository;
        _currentUserService = currentUserService;
    }

    public async Task<RoomDto> Handle(GetRoomByIdQuery request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdWithDetailsAsync(request.RoomId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (!_currentUserService.IsInRole("Admin") && room.Hotel.OwnerId != _currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view rooms for your own hotel.");

        return new RoomDto(room.Id, room.HotelId, room.Number, room.RoomType.ToString(),
            room.AdultCapacity, room.ChildCapacity, room.BasePrice.Amount, room.BasePrice.Currency, room.IsActive,
            room.CreatedAt, room.ModifiedAt);
    }
}