using Application.Features.HotelVisits.Commands.RecordHotelVisit;
using Application.Features.HotelVisits.Common;
using Application.Features.HotelVisits.Queries.GetRecentlyVisited;
using Application.Features.HotelVisits.Queries.GetTrendingCities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/hotel-visits")]
public class HotelVisitsController(ISender sender) : ControllerBase
{
    // Anonymous-friendly: the handler records the current user id, or null for a guest.
    [HttpPost("{hotelId:int}")]
    public async Task<IActionResult> Record(int hotelId, CancellationToken cancellationToken)
    {
        await sender.Send(new RecordHotelVisitCommand(hotelId), cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("recently-visited")]
    public async Task<ActionResult<IReadOnlyList<RecentlyVisitedDto>>> RecentlyVisited(
        [FromQuery] int count = 5, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetRecentlyVisitedQuery(count), cancellationToken));

    [HttpGet("trending-cities")]
    public async Task<ActionResult<IReadOnlyList<TrendingCityDto>>> TrendingCities(
        [FromQuery] int count = 5, CancellationToken cancellationToken = default)
        => Ok(await sender.Send(new GetTrendingCitiesQuery(count), cancellationToken));
}
