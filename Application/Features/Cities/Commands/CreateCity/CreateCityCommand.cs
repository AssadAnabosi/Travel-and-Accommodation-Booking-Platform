using Application.Common.Security;
using Application.Features.Cities.Common;
using MediatR;

namespace Application.Features.Cities.Commands.CreateCity;

[Authorize(Roles = "Admin")]
public record CreateCityCommand(string Name, string Country, string PostOffice) : IRequest<CityDto>;