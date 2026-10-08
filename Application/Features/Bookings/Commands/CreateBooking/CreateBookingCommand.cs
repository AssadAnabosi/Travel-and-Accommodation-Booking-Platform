using MediatR;

namespace Application.Features.Bookings.Commands.CreateBooking;

// UserId is intentionally NOT a field here — it comes from ICurrentUserService in the handler,
// never trusted from the client payload.
public record CreateBookingCommand(
    int RoomId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    string? SpecialRequests) : IRequest<CreateBookingResponse>;