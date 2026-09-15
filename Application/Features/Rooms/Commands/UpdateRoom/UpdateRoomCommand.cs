using Application.Common.Security;
using Application.Features.Rooms.Common;
using MediatR;

namespace Application.Features.Rooms.Commands.UpdateRoom;

[Authorize(Roles = "Admin,HotelOwner")]
public record UpdateRoomCommand(int RoomId, int AdultCapacity, int ChildCapacity) : IRequest<RoomDto>;