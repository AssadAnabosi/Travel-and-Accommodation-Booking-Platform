using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Amenities.Commands.CreateAmenity;

public class CreateAmenityCommandValidator : AbstractValidator<CreateAmenityCommand>
{
    private readonly IAmenityRepository _amenityRepository;

    public CreateAmenityCommandValidator(IAmenityRepository amenityRepository)
    {
        _amenityRepository = amenityRepository;

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(100)
            .MustAsync(BeUnique).WithMessage("An amenity with this name already exists.");
    }

    private async Task<bool> BeUnique(string name, CancellationToken cancellationToken)
    {
        var exists = await _amenityRepository.NameExistsAsync(name, excludeId: null, cancellationToken);
        return !exists;
    }
}