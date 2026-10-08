using Application.Common.Models;
using Application.Features.Hotels.Commands.AddHotelImage;
using Application.Features.Hotels.Commands.ApproveHotel;
using Application.Features.Hotels.Commands.CreateHotel;
using Application.Features.Hotels.Commands.DeleteHotel;
using Application.Features.Hotels.Commands.RejectHotel;
using Application.Features.Hotels.Commands.RemoveHotelImage;
using Application.Features.Hotels.Commands.UpdateHotel;
using Application.Features.Hotels.Commands.UpdateHotelAmenities;
using Application.Features.Hotels.Common;
using Application.Features.Hotels.Queries.GetFeaturedDeals;
using Application.Features.Hotels.Queries.GetHotelById;
using Application.Features.Hotels.Queries.GetHotelDetail;
using Application.Features.Hotels.Queries.GetMyHotels;
using Application.Features.Hotels.Queries.GetPendingHotels;
using Application.Features.Hotels.Queries.SearchHotels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Hotels - Management")]
public class HotelsController(ISender sender) : ControllerBase
{
    [HttpGet("search")]
    [Tags("Hotels - Public discovery")]
    public async Task<ActionResult<PaginatedList<HotelSearchResultDto>>> Search(
        [FromQuery] SearchHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("featured-deals")]
    [Tags("Hotels - Public discovery")]
    public async Task<ActionResult<IReadOnlyList<FeaturedDealDto>>> FeaturedDeals(
        [FromQuery] GetFeaturedDealsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("{id:int}")]
    [Tags("Hotels - Public discovery")]
    public async Task<ActionResult<HotelDetailDto>> GetDetail(
        int id, [FromQuery] DateOnly? checkIn, [FromQuery] DateOnly? checkOut, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetHotelDetailQuery(id, checkIn, checkOut), cancellationToken));
    

    [Authorize(Roles = "HotelOwner")]
    [HttpGet("mine")]
    public async Task<ActionResult<PaginatedList<HotelDto>>> GetMine(
        [FromQuery] GetMyHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<ActionResult<PaginatedList<HotelDto>>> GetPending(
        [FromQuery] GetPendingHotelsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpGet("{id:int}/manage")]
    public async Task<ActionResult<HotelDto>> GetManage(int id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetHotelByIdQuery(id), cancellationToken));

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost]
    public async Task<ActionResult<HotelDto>> Create(CreateHotelCommand command, CancellationToken cancellationToken)
    {
        var hotel = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetManage), new { id = hotel.Id }, hotel);
    }

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<HotelDto>> Update(
        int id, UpdateHotelCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { HotelId = id }, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteHotelCommand(id), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new ApproveHotelCommand(id), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectHotelCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { HotelId = id }, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPut("{id:int}/amenities")]
    public async Task<IActionResult> UpdateAmenities(
        int id, UpdateHotelAmenitiesCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command with { HotelId = id }, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost("{id:int}/images")]
    public async Task<IActionResult> AddImage(
        int id, AddHotelImageCommand command, CancellationToken cancellationToken)
    {
        var imageId = await sender.Send(command with { HotelId = id }, cancellationToken);
        return CreatedAtAction(nameof(GetManage), new { id }, new { imageId });
    }

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpDelete("{id:int}/images/{imageId:int}")]
    public async Task<IActionResult> RemoveImage(int id, int imageId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveHotelImageCommand(id, imageId), cancellationToken);
        return NoContent();
    }
}