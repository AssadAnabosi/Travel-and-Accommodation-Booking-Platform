using Application.Common.Security;
using Application.Features.Amenities.Common;
using MediatR;

namespace Application.Features.Amenities.Commands.CreateAmenity;

[Authorize(Roles = "Admin,HotelOwner")]
public record CreateAmenityCommand(string Name) : IRequest<AmenityDto>;