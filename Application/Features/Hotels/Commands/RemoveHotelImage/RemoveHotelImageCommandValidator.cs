using FluentValidation;

namespace Application.Features.Hotels.Commands.RemoveHotelImage;

public class RemoveHotelImageCommandValidator : AbstractValidator<RemoveHotelImageCommand>
{
    public RemoveHotelImageCommandValidator()
    {
        RuleFor(x => x.HotelId).GreaterThan(0);
        RuleFor(x => x.ImageId).GreaterThan(0);
    }
}