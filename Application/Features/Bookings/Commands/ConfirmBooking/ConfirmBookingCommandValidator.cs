using FluentValidation;

namespace Application.Features.Bookings.Commands.ConfirmBooking;

public class ConfirmBookingCommandValidator : AbstractValidator<ConfirmBookingCommand>
{
    public ConfirmBookingCommandValidator()
    {
        RuleFor(x => x.BookingId).NotEmpty();
        RuleFor(x => x.CardToken).NotEmpty();
    }
}