using Application.Common.Security;
using MediatR;

namespace Application.Features.Hotels.Commands.ReassignHotelOwner;

[Authorize(Roles = "Admin")]
public record ReassignHotelOwnerCommand(int HotelId, Guid NewOwnerId) : IRequest;
