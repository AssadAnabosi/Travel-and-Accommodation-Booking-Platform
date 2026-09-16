using Application.Common.Security;
using Application.Features.Bookings.Common;
using MediatR;

namespace Application.Features.Bookings.Queries.GetBookingById;

[Authorize]
public record GetBookingByIdQuery(Guid BookingId) : IRequest<BookingDetailDto>;