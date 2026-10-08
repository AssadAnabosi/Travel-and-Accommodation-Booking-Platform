namespace Application.Features.Bookings.Common;

public record BookingListItemDto(
    Guid Id,
    string ConfirmationNumber,
    string HotelName,
    string RoomNumber,
    DateOnly CheckIn,
    DateOnly CheckOut,
    string Status,
    decimal TotalPrice,
    string Currency);