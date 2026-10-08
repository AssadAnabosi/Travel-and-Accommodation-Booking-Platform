using Application.Features.Amenities.Commands.CreateAmenity;
using Application.Features.Amenities.Commands.DeleteAmenity;
using Application.Features.Amenities.Commands.UpdateAmenity;
using Application.Features.Amenities.Common;
using Application.Features.Amenities.Queries.GetAmenities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Amenities - Management")]
public class AmenitiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Tags("Amenities - Public")]
    public async Task<ActionResult<IReadOnlyList<AmenityDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetAmenitiesQuery(), cancellationToken));

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost]
    public async Task<ActionResult<AmenityDto>> Create(CreateAmenityCommand command, CancellationToken cancellationToken)
    {
        var amenity = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAll), null, amenity);
    }

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AmenityDto>> Update(
        int id, UpdateAmenityCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { AmenityId = id }, cancellationToken));

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAmenityCommand(id), cancellationToken);
        return NoContent();
    }
}
