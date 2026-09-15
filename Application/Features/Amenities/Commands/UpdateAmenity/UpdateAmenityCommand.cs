using Application.Common.Security;
using Application.Features.Amenities.Common;
using MediatR;

namespace Application.Features.Amenities.Commands.UpdateAmenity;

[Authorize(Roles = "Admin,HotelOwner")]
public record UpdateAmenityCommand(int AmenityId, string Name) : IRequest<AmenityDto>;