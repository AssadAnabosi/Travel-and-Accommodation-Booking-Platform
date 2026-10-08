using Application.Common.Security;
using MediatR;

namespace Application.Features.Cities.Commands.DeleteCity;

[Authorize(Roles = "Admin")]
public record DeleteCityCommand(int CityId) : IRequest;