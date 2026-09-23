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

/// <summary>Hotel reviews — public read; verified-stay-gated create, author-only edit, author/Admin delete.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Reviews - Management")]
public class ReviewsController(ISender sender) : ControllerBase
{
    /// <summary>Lists a hotel's reviews (paginated).</summary>
    [HttpGet("by-hotel/{hotelId:int}")]
    [Tags("Reviews - Public")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedList<ReviewDto>>> GetByHotel(
        int hotelId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetReviewsByHotelQuery(hotelId, pageNumber, pageSize), cancellationToken));

    /// <summary>Reviews a hotel after a completed stay; 400 without one or if already reviewed (409 only on a concurrent duplicate).</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetByHotel), new { hotelId = review.HotelId }, review);
    }

    /// <summary>Edits one of the current user's reviews.</summary>
    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> Update(
        int id, UpdateReviewCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { ReviewId = id }, cancellationToken));

    /// <summary>Deletes a review (its author or an admin).</summary>
    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteReviewCommand(id), cancellationToken);
        return NoContent();
    }
}
