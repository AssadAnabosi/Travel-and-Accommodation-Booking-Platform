using Application.Common.Models;
using Application.Features.Bookings.Commands.CheckInBooking;
using Application.Features.Bookings.Commands.CheckOutBooking;
using Application.Features.Bookings.Commands.ConfirmBooking;
using Application.Features.Bookings.Commands.CreateBooking;
using Application.Features.Bookings.Common;
using Application.Features.Bookings.Queries.GetBookingById;
using Application.Features.Bookings.Queries.GetBookingConfirmationPdf;
using Application.Features.Bookings.Queries.GetMyBookings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>Bookings — create, confirm/pay (mock gateway → PDF + email), check-in/out, list, and the confirmation PDF.</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(ISender sender) : ControllerBase
{
    /// <summary>Creates a pending booking for a room; 409 if the room is not available for the dates.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateBookingResponse>> Create(
        CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.BookingId }, response);
    }

    /// <summary>Pays for and confirms a pending booking (mock gateway; 402 on a failed payment, 409 if not awaiting payment) and emails the confirmation PDF.</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingDto>> Confirm(
        Guid id, ConfirmBookingCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { BookingId = id }, cancellationToken));

    /// <summary>Checks a confirmed booking in (hotel owner or admin); 409 if it isn't confirmed.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost("{id:guid}/check-in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckInBookingCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Checks a checked-in booking out (hotel owner or admin); 409 if it isn't checked in.</summary>
    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost("{id:guid}/check-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CheckOut(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckOutBookingCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Lists the current user's bookings (paginated).</summary>
    [HttpGet("mine")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedList<BookingListItemDto>>> GetMine(
        [FromQuery] GetMyBookingsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Gets a booking's details (its guest, the hotel's owner, or an admin).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetBookingByIdQuery(id), cancellationToken));

    /// <summary>Downloads the confirmation PDF; 409 if the booking is not confirmed yet.</summary>
    [HttpGet("{id:guid}/confirmation-pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetConfirmationPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdf = await sender.Send(new GetBookingConfirmationPdfQuery(id), cancellationToken);
        return File(pdf, "application/pdf", $"booking-{id}.pdf");
    }
}
