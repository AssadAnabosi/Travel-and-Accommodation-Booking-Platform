using Application.Common.Security;
using MediatR;

namespace Application.Features.Rooms.Commands.RemoveRoomImage;

[Authorize(Roles = "Admin,HotelOwner")]
public record RemoveRoomImageCommand(int RoomId, int ImageId) : IRequest;
