using Application.Common.Security;
using MediatR;

namespace Application.Features.Rooms.Commands.AddRoomImage;

[Authorize(Roles = "Admin,HotelOwner")]
public record AddRoomImageCommand(int RoomId, string Url) : IRequest<int>; // returns new RoomImage.Id
