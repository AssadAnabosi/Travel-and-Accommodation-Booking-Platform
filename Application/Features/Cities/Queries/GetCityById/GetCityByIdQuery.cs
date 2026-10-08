using Application.Features.Cities.Common;
using MediatR;

namespace Application.Features.Cities.Queries.GetCityById;

public record GetCityByIdQuery(int CityId) : IRequest<CityDto>;