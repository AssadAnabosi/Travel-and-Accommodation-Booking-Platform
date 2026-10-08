using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Cities.Queries.GetCityById;

public class GetCityByIdQueryHandler(ICityRepository cityRepository) : IRequestHandler<GetCityByIdQuery, CityDto>
{
    public async Task<CityDto> Handle(GetCityByIdQuery request, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetByIdAsync(request.CityId, cancellationToken)
                   ?? throw new NotFoundException(nameof(City), request.CityId);

        return new CityDto(city.Id, city.Name, city.Country, city.PostOffice, city.ThumbnailUrl, city.Hotels.Count, city.CreatedAt,
            city.ModifiedAt);
    }
}