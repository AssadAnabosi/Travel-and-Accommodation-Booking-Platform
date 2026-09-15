using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Cities.Commands.UpdateCity;

public class UpdateCityCommandValidator : AbstractValidator<UpdateCityCommand>
{
    private readonly ICityRepository _cityRepository;

    public UpdateCityCommandValidator(ICityRepository cityRepository)
    {
        _cityRepository = cityRepository;

        RuleFor(x => x.CityId).GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(150)
            .MustAsync(BeUniqueName).WithMessage("A city with this name already exists.");

        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostOffice).NotEmpty().MaximumLength(20);
    }

    private async Task<bool> BeUniqueName(UpdateCityCommand command, string name, CancellationToken cancellationToken)
    {
        var exists = await _cityRepository.NameExistsAsync(name, command.CityId, cancellationToken);
        return !exists;
    }
}