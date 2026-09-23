using Domain.Enums;

namespace Application.Common.Models;

public record HotelBookingFilter(
    BookingStatus? Status,
    DateOnly? CheckInFrom,
    DateOnly? CheckInTo,
    string? Keyword);
