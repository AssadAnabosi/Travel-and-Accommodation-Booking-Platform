using Application.Features.HotelVisits.Common;
using MediatR;

namespace Application.Features.HotelVisits.Queries.GetTrendingCities;

public record GetTrendingCitiesQuery(int Count = 5) : IRequest<IReadOnlyList<TrendingCityDto>>;