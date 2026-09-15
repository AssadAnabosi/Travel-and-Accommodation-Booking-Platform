using Application.Common.Security;
using Application.Features.Rooms.Common;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomsByHotel;

[Authorize(Roles = "Admin,HotelOwner")]
public record GetRoomsByHotelQuery(int HotelId) : IRequest<IReadOnlyList<RoomDto>>;