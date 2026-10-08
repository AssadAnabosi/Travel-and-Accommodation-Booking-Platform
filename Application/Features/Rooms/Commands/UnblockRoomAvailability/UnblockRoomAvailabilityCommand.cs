using Application.Common.Security;
using MediatR;

namespace Application.Features.Rooms.Commands.UnblockRoomAvailability;

[Authorize(Roles = "Admin,HotelOwner")]
public record UnblockRoomAvailabilityCommand(int RoomId, int AvailabilityId) : IRequest;