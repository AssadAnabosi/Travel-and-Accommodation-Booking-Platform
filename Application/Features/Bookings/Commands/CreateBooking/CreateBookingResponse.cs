namespace Application.Features.Bookings.Commands.CreateBooking;

public record CreateBookingResponse(
    Guid BookingId,
    string ConfirmationNumber,
    decimal TotalPrice,
    string Currency,
    string Status);