namespace Application.Features.Bookings.Common;

public record BookingDetailDto(
    Guid Id,
    string ConfirmationNumber,
    string Status,
    int HotelId,
    string HotelName,
    string HotelAddress,
    string RoomNumber,
    string RoomType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    int Adults,
    int Children,
    decimal TotalPrice,
    string Currency,
    string? SpecialRequests,
    DateTime CreatedAt);