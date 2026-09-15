using FluentValidation;

namespace Application.Features.Hotels.Commands.RejectHotel;

public class RejectHotelCommandValidator : AbstractValidator<RejectHotelCommand>
{
    public RejectHotelCommandValidator()
    {
        RuleFor(x => x.HotelId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}