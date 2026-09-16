using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Bookings.Common;
using MediatR;

namespace Application.Features.Bookings.Queries.GetMyBookings;

[Authorize]
public record GetMyBookingsQuery(int PageNumber = 1, int PageSize = 20) : IRequest<PaginatedList<BookingListItemDto>>;