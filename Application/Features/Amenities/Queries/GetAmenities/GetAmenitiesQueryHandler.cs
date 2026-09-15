using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Common;
using MediatR;

namespace Application.Features.Amenities.Queries.GetAmenities;

public class GetAmenitiesQueryHandler(IAmenityRepository amenityRepository)
    : IRequestHandler<GetAmenitiesQuery, IReadOnlyList<AmenityDto>>
{
    public async Task<IReadOnlyList<AmenityDto>> Handle(GetAmenitiesQuery request, CancellationToken cancellationToken)
    {
        var amenities = await amenityRepository.GetAllAsync(cancellationToken);
        return amenities.Select(a => new AmenityDto(a.Id, a.Name, a.CreatedAt, a.ModifiedAt)).ToList();
    }
}