using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Cities.Common;
using MediatR;

namespace Application.Features.Cities.Queries.GetCities;

public class GetCitiesQueryHandler(ICityRepository cityRepository)
    : IRequestHandler<GetCitiesQuery, PaginatedList<CityDto>>
{
    public async Task<PaginatedList<CityDto>> Handle(GetCitiesQuery request, CancellationToken cancellationToken)
    {
        var result =
            await cityRepository.SearchAsync(request.Keyword, request.PageNumber, request.PageSize, cancellationToken);

        var items = result.Items
            .Select(c => new CityDto(c.Id, c.Name, c.Country, c.PostOffice, c.ThumbnailUrl, c.Hotels.Count, c.CreatedAt,
                c.ModifiedAt))
            .ToList();

        return new PaginatedList<CityDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}