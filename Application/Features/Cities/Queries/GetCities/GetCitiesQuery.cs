using Application.Common.Models;
using Application.Features.Cities.Common;
using MediatR;

namespace Application.Features.Cities.Queries.GetCities;

public record GetCitiesQuery(string? Keyword, int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<CityDto>>;