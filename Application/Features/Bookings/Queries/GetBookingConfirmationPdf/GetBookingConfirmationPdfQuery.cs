using Application.Common.Security;
using MediatR;

namespace Application.Features.Bookings.Queries.GetBookingConfirmationPdf;

[Authorize]
public record GetBookingConfirmationPdfQuery(Guid BookingId) : IRequest<byte[]>;