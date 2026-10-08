using Application.Common.Security;
using MediatR;

namespace Application.Features.Rooms.Commands.DeleteRoom;

[Authorize(Roles = "Admin,HotelOwner")]
public record DeleteRoomCommand(int RoomId) : IRequest;