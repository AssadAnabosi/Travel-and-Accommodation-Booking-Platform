using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Amenities.Commands.UpdateAmenity;

public class UpdateAmenityCommandValidator : AbstractValidator<UpdateAmenityCommand>
{
    private readonly IAmenityRepository _amenityRepository;

    public UpdateAmenityCommandValidator(IAmenityRepository amenityRepository)
    {
        _amenityRepository = amenityRepository;

        RuleFor(x => x.AmenityId).GreaterThan(0);
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(100)
            .MustAsync(BeUnique).WithMessage("An amenity with this name already exists.");
    }

    private async Task<bool> BeUnique(UpdateAmenityCommand command, string name, CancellationToken cancellationToken)
    {
        var exists = await _amenityRepository.NameExistsAsync(name, command.AmenityId, cancellationToken);
        return !exists;
    }
}