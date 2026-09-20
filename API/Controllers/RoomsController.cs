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

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,HotelOwner")]
public class RoomsController(ISender sender) : ControllerBase
{
    [HttpGet("by-hotel/{hotelId:int}")]
    public async Task<ActionResult<IReadOnlyList<RoomDto>>> GetByHotel(int hotelId, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRoomsByHotelQuery(hotelId), cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoomDto>> GetById(int id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetRoomByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomCommand command, CancellationToken cancellationToken)
    {
        var room = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, room);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoomDto>> Update(int id, UpdateRoomCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { RoomId = id }, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteRoomCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/availability/block")]
    public async Task<IActionResult> BlockAvailability(
        int id, BlockRoomAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var availabilityId = await sender.Send(command with { RoomId = id }, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { availabilityId });
    }

    [HttpDelete("{id:int}/availability/{availabilityId:int}")]
    public async Task<IActionResult> UnblockAvailability(
        int id, int availabilityId, CancellationToken cancellationToken)
    {
        await sender.Send(new UnblockRoomAvailabilityCommand(id, availabilityId), cancellationToken);
        return NoContent();
    }
}
