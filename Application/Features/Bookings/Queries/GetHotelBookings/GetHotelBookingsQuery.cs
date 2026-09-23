using Application.Common.Models;
using Application.Common.Security;
using Application.Features.Bookings.Common;
using Domain.Enums;
using MediatR;

namespace Application.Features.Bookings.Queries.GetHotelBookings;

/// <summary>
/// A hotel's bookings for its owner (or an admin), ordered by check-in date. E.g. today's arrivals:
/// Status = Confirmed, CheckInFrom = CheckInTo = today; guests in house: Status = CheckedIn.
/// </summary>
[Authorize(Roles = "Admin,HotelOwner")]
public record GetHotelBookingsQuery(
    int HotelId,
    BookingStatus? Status = null,
    DateOnly? CheckInFrom = null,
    DateOnly? CheckInTo = null,
    string? Keyword = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<HotelBookingListItemDto>>;
