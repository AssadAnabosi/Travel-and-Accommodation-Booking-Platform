using Application.Common.Models;
using Application.Features.Reviews.Commands.CreateReview;
using Application.Features.Reviews.Commands.DeleteReview;
using Application.Features.Reviews.Commands.UpdateReview;
using Application.Features.Reviews.Common;
using Application.Features.Reviews.Queries.GetReviewsByHotel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Reviews - Management")]
public class ReviewsController(ISender sender) : ControllerBase
{
    [HttpGet("by-hotel/{hotelId:int}")]
    [Tags("Reviews - Public")]
    public async Task<ActionResult<PaginatedList<ReviewDto>>> GetByHotel(
        int hotelId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetReviewsByHotelQuery(hotelId, pageNumber, pageSize), cancellationToken));

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetByHotel), new { hotelId = review.HotelId }, review);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReviewDto>> Update(
        int id, UpdateReviewCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { ReviewId = id }, cancellationToken));

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteReviewCommand(id), cancellationToken);
        return NoContent();
    }
}
