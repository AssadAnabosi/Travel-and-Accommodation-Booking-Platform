namespace Application.Features.Bookings.Common;

public record BookingDto(Guid Id, string ConfirmationNumber, string Status, decimal TotalPrice, string Currency, DateOnly CheckIn, DateOnly CheckOut);