using Application.Common.Security;
using Application.Features.Cities.Common;
using MediatR;

namespace Application.Features.Cities.Commands.UpdateCity;

[Authorize(Roles = "Admin")]
public record UpdateCityCommand(
    int CityId,
    string Name,
    string Country,
    string PostOffice,
    string? ThumbnailUrl = null) : IRequest<CityDto>;