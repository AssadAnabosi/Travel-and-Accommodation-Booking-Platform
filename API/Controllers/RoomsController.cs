using Application.Features.Rooms.Commands.BlockRoomAvailability;
using Application.Features.Rooms.Commands.CreateRoom;
using Application.Features.Rooms.Commands.DeleteRoom;
using Application.Features.Rooms.Commands.UnblockRoomAvailability;
using Application.Features.Rooms.Commands.UpdateRoom;
using Application.Features.Rooms.Common;
using Application.Features.Rooms.Queries.GetRoomById;
using Application.Features.Rooms.Queries.GetRoomsByHotel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Rooms and their availability holds (owner/admin); delete soft-deletes when booking history exists.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,HotelOwner")]
public class RoomsController(ISender sender) : ControllerBase
{
    /// <summary>Lists a hotel's rooms.</summary>
    [HttpGet("by-hotel/{hotelId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetByHotel(int hotelId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRoomsByHotelQuery(hotelId), cancellationToken));

    /// <summary>Gets a room by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRoomByIdQuery(id), cancellationToken));

    /// <summary>Adds a room to a hotel.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    /// <summary>Updates a room.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomDto>> Update(int id, UpdateRoomCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { RoomId = id }, cancellationToken));

    /// <summary>Deletes a room — soft delete if it has any booking history, hard delete otherwise; 409 if already deleted.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteRoomCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Blocks a date range on a room; 409 if it overlaps a booking or an existing block.</summary>
    [HttpPost("{id:int}/availability/block")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BlockAvailability(
        int id, BlockRoomAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var availabilityId = await sender.Send(command with { RoomId = id }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { availabilityId });
    }

    /// <summary>Removes an availability block from a room; 409 if the range is held by a booking, not a manual block.</summary>
    [HttpDelete("{id:int}/availability/{availabilityId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnblockAvailability(
        int id, int availabilityId, CancellationToken cancellationToken)
    {
        await sender.Send(new UnblockRoomAvailabilityCommand(id, availabilityId), cancellationToken);
        return NoContent();
    }
}
