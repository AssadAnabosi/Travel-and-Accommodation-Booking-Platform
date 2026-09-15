using Application.Common.Security;
using MediatR;

namespace Application.Features.Rooms.Commands.BlockRoomAvailability;
/// <summary>
/// admin/owner maintenance hold
/// </summary>
/// <returns>returns new RoomAvailability.Id</returns>
[Authorize(Roles = "Admin,HotelOwner")]
public record BlockRoomAvailabilityCommand(int RoomId, DateOnly StartDate, DateOnly EndDate)
    : IRequest<int>;