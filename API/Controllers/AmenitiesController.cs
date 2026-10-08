using Application.Features.Amenities.Commands.CreateAmenity;
using Application.Features.Amenities.Commands.DeleteAmenity;
using Application.Features.Amenities.Commands.UpdateAmenity;
using Application.Features.Amenities.Common;
using Application.Features.Amenities.Queries.GetAmenities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Amenities — public read, admin/owner management (delete blocked while assigned to hotels).</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Amenities - Management")]
public class AmenitiesController(ISender sender) : ControllerBase
{
    /// <summary>Lists all amenities.</summary>
    [HttpGet]
    [Tags("Amenities - Public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AmenityDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAmenitiesQuery(), cancellationToken));

    /// <summary>Creates an amenity.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<AmenityDto>> Create(CreateAmenityCommand command, CancellationToken cancellationToken)
    {
        var amenity = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), null, amenity);
    }

    /// <summary>Updates an amenity's name/description.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AmenityDto>> Update(
        int id, UpdateAmenityCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { AmenityId = id }, cancellationToken));

    /// <summary>Deletes an amenity; 409 while it is still assigned to any hotel.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAmenityCommand(id), cancellationToken);
        return NoContent();
    }
}
