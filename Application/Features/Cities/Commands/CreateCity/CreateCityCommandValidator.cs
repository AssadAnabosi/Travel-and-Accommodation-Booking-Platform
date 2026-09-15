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
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var exists = await _cityRepository.NameExistsAsync(name, excludeId: null, cancellationToken);
        return !exists;
    }
}