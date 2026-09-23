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

/// <summary>Room discounts — HotelOwner-managed (overlap-checked); admin/owner read.</summary>
[ApiController]
[Route("api/[controller]")]
public class DiscountsController(ISender sender) : ControllerBase
{
    /// <summary>Lists a room's discounts.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpGet("by-room/{roomId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<DiscountDto>>> GetByRoom(int roomId,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetDiscountsByRoomQuery(roomId), cancellationToken));

    /// <summary>Creates a discount on one of the owner's rooms.</summary>
    [Authorize(Roles = "HotelOwner")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountDto>> Create(
        CreateDiscountCommand command, CancellationToken cancellationToken)
    {
        var discount = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetByRoom), new { roomId = discount.RoomId }, discount);
    }

    /// <summary>Updates a discount.</summary>
    [Authorize(Roles = "HotelOwner")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscountDto>> Update(
        int id, UpdateDiscountCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { DiscountId = id }, cancellationToken));

    /// <summary>Deactivates a discount without deleting it.</summary>
    [Authorize(Roles = "HotelOwner")]
    [HttpPost("{id:int}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeactivateDiscountCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Deletes a discount.</summary>
    [Authorize(Roles = "HotelOwner")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDiscountCommand(id), cancellationToken);
        return NoContent();
    }
}
