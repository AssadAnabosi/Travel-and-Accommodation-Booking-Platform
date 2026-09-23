namespace Application.Features.Bookings.Common;

/// <summary>One row of a hotel's booking list (the owner/admin front desk).</summary>
public record HotelBookingListItemDto(
    Guid Id,
    string ConfirmationNumber,
    Guid GuestId,
    string GuestName,
    string GuestEmail,
    int RoomId,
    string RoomNumber,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    string Status,
    decimal TotalPrice,
    string Currency,
    string? SpecialRequests,
    DateTime CreatedAt);
