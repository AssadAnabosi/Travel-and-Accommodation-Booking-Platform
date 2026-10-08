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

/// <summary>Cities — public read, admin-managed CRUD (delete blocked while hotels reference the city).</summary>
[ApiController]
[Route("api/[controller]")]
public class CitiesController(ISender sender) : ControllerBase
{
    /// <summary>Lists cities (paginated, optional search).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<CityDto>>> GetCities(
        [FromQuery] GetCitiesQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Gets a city by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityDto>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetCityByIdQuery(id), cancellationToken));

    /// <summary>Creates a city.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<CityDto>> Create(CreateCityCommand command, CancellationToken cancellationToken)
    {
        var city = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = city.Id }, city);
    }

    /// <summary>Updates a city.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CityDto>> Update(int id, UpdateCityCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { CityId = id }, cancellationToken));

    /// <summary>Deletes a city; 409 while any hotel references it.</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCityCommand(id), cancellationToken);
        return NoContent();
    }
}
