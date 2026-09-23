using Application.Features.HotelVisits.Commands.RecordHotelVisit;
using Application.Features.HotelVisits.Common;
using Application.Features.HotelVisits.Queries.GetRecentlyVisited;
using Application.Features.HotelVisits.Queries.GetTrendingCities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Hotel-visit tracking and discovery — record a visit (anonymous-friendly), recently visited, trending cities.</summary>
[ApiController]
[Route("api/hotel-visits")]
public class HotelVisitsController(ISender sender) : ControllerBase
{
    /// <summary>Records a visit to a hotel page (attributed to the current user when signed in).</summary>
    // Anonymous-friendly: the handler records the current user id, or null for a guest.
    [HttpPost("{hotelId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Record(int hotelId, CancellationToken cancellationToken)
    {
        await sender.Send(new RecordHotelVisitCommand(hotelId), cancellationToken);
        return NoContent();
    }

    /// <summary>Lists the current user's most recently visited hotels.</summary>
    [Authorize]
    [HttpGet("recently-visited")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecentlyVisitedDto>>> RecentlyVisited(
        [FromQuery] int count = 5, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetRecentlyVisitedQuery(count), cancellationToken));

    /// <summary>Lists the most visited cities.</summary>
    [HttpGet("trending-cities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrendingCityDto>>> TrendingCities(
        [FromQuery] int count = 5, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetTrendingCitiesQuery(count), cancellationToken));
}
