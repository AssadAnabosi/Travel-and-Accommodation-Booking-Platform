using Application.Common.Models;
using Application.Features.Hotels.Commands.AddHotelImage;
using Application.Features.Hotels.Commands.ApproveHotel;
using Application.Features.Hotels.Commands.CreateHotel;
using Application.Features.Hotels.Commands.DeleteHotel;
using Application.Features.Hotels.Commands.ReassignHotelOwner;
using Application.Features.Hotels.Commands.RejectHotel;
using Application.Features.Hotels.Commands.RemoveHotelImage;
using Application.Features.Hotels.Commands.UpdateHotel;
using Application.Features.Hotels.Commands.UpdateHotelAmenities;
using Application.Features.Hotels.Common;
using Application.Features.Hotels.Queries.GetFeaturedDeals;
using Application.Features.Hotels.Queries.GetHotelById;
using Application.Features.Hotels.Queries.GetHotelDetail;
using Application.Features.Hotels.Queries.GetHotels;
using Application.Features.Hotels.Queries.GetMyHotels;
using Application.Features.Hotels.Queries.GetPendingHotels;
using Application.Features.Hotels.Queries.SearchHotels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Hotels — public search/discovery and owner/admin management with the approval workflow.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Hotels - Management")]
public class HotelsController(ISender sender) : ControllerBase
{
    /// <summary>Searches approved hotels by keyword, city, dates, occupancy, price, stars, room type and amenities (paginated).</summary>
    [HttpGet("search")]
    [Tags("Hotels - Public discovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<HotelSearchResultDto>>> Search(
        [FromQuery] SearchHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Lists the best currently discounted rooms (the home-page featured deals).</summary>
    [HttpGet("featured-deals")]
    [Tags("Hotels - Public discovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeaturedDealDto>>> FeaturedDeals(
        [FromQuery] GetFeaturedDealsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Gets an approved hotel's public details; pass dates to get room availability for that stay.</summary>
    [HttpGet("{id:int}")]
    [Tags("Hotels - Public discovery")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelDetailDto>> GetDetail(
        int id, [FromQuery] DateOnly? checkIn, [FromQuery] DateOnly? checkOut, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetHotelDetailQuery(id, checkIn, checkOut), cancellationToken));


    /// <summary>Lists the current owner's hotels, in any approval state (paginated).</summary>
    [Authorize(Roles = "HotelOwner")]
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<HotelDto>>> GetMine(
        [FromQuery] GetMyHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Lists every hotel in any approval state for the admin grid (paginated; filter by keyword, city, status, owner).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<HotelDto>>> GetAll(
        [FromQuery] GetHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Lists hotels awaiting approval (paginated).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<HotelDto>>> GetPending(
        [FromQuery] GetPendingHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Gets a hotel's management view, in any approval state (its owner or an admin).</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpGet("{id:int}/manage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelDto>> GetManage(int id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetHotelByIdQuery(id), cancellationToken));

    /// <summary>Creates a hotel; it starts pending admin approval.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult<HotelDto>> Create(CreateHotelCommand command, CancellationToken cancellationToken)
    {
        var hotel = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetManage), new { id = hotel.Id }, hotel);
    }

    /// <summary>Updates a hotel's details.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HotelDto>> Update(
        int id, UpdateHotelCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { HotelId = id }, cancellationToken));

    /// <summary>Deletes a hotel; 409 while it still has rooms.</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteHotelCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Approves a pending hotel, making it publicly visible; 409 if already approved.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveHotelCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Rejects a pending hotel with a reason.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(int id, RejectHotelCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { HotelId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Reassigns a hotel to another owner (Admin); 400 unless the new owner is an active HotelOwner.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReassignOwner(
        int id, ReassignHotelOwnerCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { HotelId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Replaces a hotel's amenity set.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPut("{id:int}/amenities")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAmenities(
        int id, UpdateHotelAmenitiesCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { HotelId = id }, cancellationToken);
        return NoContent();
    }

    /// <summary>Adds an image to a hotel; returns the new <c>imageId</c>.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost("{id:int}/images")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImage(
        int id, AddHotelImageCommand command, CancellationToken cancellationToken)
    {
        var imageId = await sender.Send(command with { HotelId = id }, cancellationToken);
        return CreatedAtAction(nameof(GetManage), new { id }, new { imageId });
    }

    /// <summary>Removes an image from a hotel.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveHotelImageCommand(id, imageId), cancellationToken);
        return NoContent();
    }
}
