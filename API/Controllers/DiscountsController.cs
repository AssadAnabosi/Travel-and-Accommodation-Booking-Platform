using Application.Features.Discounts.Commands.CreateDiscount;
using Application.Features.Discounts.Commands.DeactivateDiscount;
using Application.Features.Discounts.Commands.DeleteDiscount;
using Application.Features.Discounts.Commands.UpdateDiscount;
using Application.Features.Discounts.Common;
using Application.Features.Discounts.Queries.GetDiscountsByRoom;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpGet("by-room/{roomId:int}")]
    public async Task<ActionResult<IReadOnlyList<DiscountDto>>> GetByRoom(int roomId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetDiscountsByRoomQuery(roomId), cancellationToken));

    [Authorize(Roles = "HotelOwner")]
    [HttpPost]
    public async Task<ActionResult<DiscountDto>> Create(
        CreateDiscountCommand command, CancellationToken cancellationToken)
    {
        var discount = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetByRoom), new { roomId = discount.RoomId }, discount);
    }

    [Authorize(Roles = "HotelOwner")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DiscountDto>> Update(
        int id, UpdateDiscountCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { DiscountId = id }, cancellationToken));

    [Authorize(Roles = "HotelOwner")]
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateDiscountCommand(id), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "HotelOwner")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDiscountCommand(id), cancellationToken);
        return NoContent();
    }
}