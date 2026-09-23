using Application.Common.Interfaces.Persistence;
using Application.Features.HotelVisits.Common;
using MediatR;

namespace Application.Features.HotelVisits.Queries.GetTrendingCities;

public class GetTrendingCitiesQueryHandler(IHotelVisitRepository hotelVisitRepository)
    : IRequestHandler<GetTrendingCitiesQuery, IReadOnlyList<TrendingCityDto>>
{
    public async Task<IReadOnlyList<TrendingCityDto>> Handle(GetTrendingCitiesQuery request,
        CancellationToken cancellationToken)
    {
        var trending = await hotelVisitRepository.GetTrendingCitiesAsync(request.Count, cancellationToken);
        return trending.Select(t => new TrendingCityDto(t.CityId, t.CityName, t.ThumbnailUrl, t.VisitCount)).ToList();
    }
}