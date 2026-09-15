using Application.Common.Security;
using Application.Features.Rooms.Common;
using MediatR;

namespace Application.Features.Rooms.Queries.GetRoomById;

[Authorize(Roles = "Admin,HotelOwner")]
public record GetRoomByIdQuery(int RoomId) : IRequest<RoomDto>;