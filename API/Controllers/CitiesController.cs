using Application.Common.Models;
using Application.Features.Cities.Commands.CreateCity;
using Application.Features.Cities.Commands.DeleteCity;
using Application.Features.Cities.Commands.UpdateCity;
using Application.Features.Cities.Common;
using Application.Features.Cities.Queries.GetCities;
using Application.Features.Cities.Queries.GetCityById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedList<CityDto>>> GetCities(
        [FromQuery] GetCitiesQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CityDto>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCityByIdQuery(id), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CityDto>> Create(CreateCityCommand command, CancellationToken cancellationToken)
    {
        var city = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = city.Id }, city);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CityDto>> Update(int id, UpdateCityCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { CityId = id }, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCityCommand(id), cancellationToken);
        return NoContent();
    }
}
