using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomsByHotel;

public class GetRoomsByHotelQueryHandler(
    IHotelRepository hotelRepository,
    IRoomRepository roomRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetRoomsByHotelQuery, IReadOnlyList<RoomDto>>
{
    public async Task<IReadOnlyList<RoomDto>> Handle(GetRoomsByHotelQuery request, CancellationToken cancellationToken)
    {
        var hotel = await hotelRepository.GetByIdAsync(request.HotelId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Hotel), request.HotelId);

        if (!currentUserService.IsInRole("Admin") && hotel.OwnerId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only view rooms for your own hotel.");

        var rooms = await roomRepository.GetByHotelIdAsync(request.HotelId, cancellationToken);

        return rooms.Select(r => new RoomDto(r.Id, r.HotelId, r.Number, r.RoomType.ToString(),
            r.AdultCapacity, r.ChildCapacity, r.BasePrice.Amount, r.BasePrice.Currency, r.IsActive, r.CreatedAt,
            r.ModifiedAt)).ToList();
    }
}