using FluentValidation;

namespace Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);

        RuleFor(x => x.CheckIn)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("Check-in date cannot be in the past.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .WithMessage("Check-out date must be after check-in date.");

        RuleFor(x => x.Adults).GreaterThan(0);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
    }
}