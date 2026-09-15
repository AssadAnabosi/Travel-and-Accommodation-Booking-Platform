using Application.Common.Security;
using Application.Features.Rooms.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Rooms.Commands.CreateRoom;

[Authorize(Roles = "Admin,HotelOwner")]
public record CreateRoomCommand(
    int HotelId,
    string Number,
    RoomType RoomType,
    int AdultCapacity,
    int ChildCapacity,
    decimal BasePrice,
    string Currency = "USD") : IRequest<RoomDto>;