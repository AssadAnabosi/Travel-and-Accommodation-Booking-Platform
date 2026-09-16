using FluentValidation;

namespace Application.Features.Bookings.Commands.CheckOutBooking;

public class CheckOutBookingCommandValidator : AbstractValidator<CheckOutBookingCommand>
{
    public CheckOutBookingCommandValidator() => RuleFor(x => x.BookingId).NotEmpty();
}