using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Cities.Commands.CreateCity;

public class CreateCityCommandValidator : AbstractValidator<CreateCityCommand>
{
    private readonly ICityRepository _cityRepository;

    public CreateCityCommandValidator(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(150)
            .MustAsync(BeUniqueName).WithMessage("A city with this name already exists.");

        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostOffice).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ThumbnailUrl!)
            .MaximumLength(2000).Must(BeAnAbsoluteHttpUrl)
            .WithMessage("ThumbnailUrl must be an absolute http(s) URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.ThumbnailUrl));
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var exists = await _cityRepository.NameExistsAsync(name, excludeId: null, cancellationToken);
        return !exists;
    }

    private static bool BeAnAbsoluteHttpUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}