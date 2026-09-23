using FluentValidation;

namespace Application.Features.Rooms.Commands.AddRoomImage;

public class AddRoomImageCommandValidator : AbstractValidator<AddRoomImageCommand>
{
    public AddRoomImageCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.Url).NotEmpty().MaximumLength(2000).Must(BeAValidUrl)
            .WithMessage("Url must be a valid absolute URL.");
    }

    private static bool BeAValidUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out _);
}
