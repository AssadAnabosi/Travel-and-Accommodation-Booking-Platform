using Application.Common.Security;
using MediatR;

namespace Application.Features.Bookings.Commands.CheckInBooking;

[Authorize(Roles = "Admin,HotelOwner")]
public record CheckInBookingCommand(Guid BookingId) : IRequest;