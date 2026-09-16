using Application.Common.Security;
using MediatR;

namespace Application.Features.Bookings.Commands.CheckOutBooking;

[Authorize(Roles = "Admin,HotelOwner")]
public record CheckOutBookingCommand(Guid BookingId) : IRequest;