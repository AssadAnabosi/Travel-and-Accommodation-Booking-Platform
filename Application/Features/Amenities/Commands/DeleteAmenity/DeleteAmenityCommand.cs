using Application.Common.Security;
using MediatR;

namespace Application.Features.Amenities.Commands.DeleteAmenity;

[Authorize(Roles = "Admin,HotelOwner")]
public record DeleteAmenityCommand(int AmenityId) : IRequest;