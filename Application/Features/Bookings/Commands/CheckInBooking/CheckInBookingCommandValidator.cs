using FluentValidation;

namespace Application.Features.Bookings.Commands.CheckInBooking;

public class CheckInBookingCommandValidator : AbstractValidator<CheckInBookingCommand>
{
    public CheckInBookingCommandValidator() => RuleFor(x => x.BookingId).NotEmpty();
}