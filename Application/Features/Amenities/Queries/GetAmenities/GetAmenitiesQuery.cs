using Application.Features.Amenities.Common;
using MediatR;

namespace Application.Features.Amenities.Queries.GetAmenities;

public record GetAmenitiesQuery : IRequest<IReadOnlyList<AmenityDto>>;