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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreateBookingResponse>> Create(
        CreateBookingCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.BookingId }, response);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<BookingDto>> Confirm(
        Guid id, ConfirmBookingCommand command, CancellationToken cancellationToken)
        => Ok(await sender.Send(command with { BookingId = id }, cancellationToken));

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost("{id:guid}/check-in")]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckInBookingCommand(id), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,HotelOwner")]
    [HttpPost("{id:guid}/check-out")]
    public async Task<IActionResult> CheckOut(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new CheckOutBookingCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<ActionResult<PaginatedList<BookingListItemDto>>> GetMine(
        [FromQuery] GetMyBookingsQuery query, CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetBookingByIdQuery(id), cancellationToken));

    [HttpGet("{id:guid}/confirmation-pdf")]
    public async Task<IActionResult> GetConfirmationPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdf = await sender.Send(new GetBookingConfirmationPdfQuery(id), cancellationToken);
        return File(pdf, "application/pdf", $"booking-{id}.pdf");
    }
}
