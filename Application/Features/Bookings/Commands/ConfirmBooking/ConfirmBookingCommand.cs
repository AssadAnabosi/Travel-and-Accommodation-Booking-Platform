using Application.Common.Security;
using Application.Features.Bookings.Common;
using MediatR;

namespace Application.Features.Bookings.Commands.ConfirmBooking;

/// <summary>
/// mock payment, then email + PDF
/// </summary>
[Authorize]
public record ConfirmBookingCommand(Guid BookingId, string CardToken) : IRequest<BookingDto>;