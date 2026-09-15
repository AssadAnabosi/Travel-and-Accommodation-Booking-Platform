using FluentValidation;

namespace Application.Features.Hotels.Commands.AddHotelImage;

public class AddHotelImageCommandValidator : AbstractValidator<AddHotelImageCommand>
{
    public AddHotelImageCommandValidator()
    {
        RuleFor(x => x.HotelId).GreaterThan(0);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000).Must(BeAValidUrl)
            .WithMessage("Url must be a valid absolute URL.");
    }

    private static bool BeAValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}