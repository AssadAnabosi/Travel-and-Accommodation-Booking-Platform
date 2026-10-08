using FluentValidation;

namespace Application.Features.Amenities.Commands.DeleteAmenity;

public class DeleteAmenityCommandValidator : AbstractValidator<DeleteAmenityCommand>
{
    public DeleteAmenityCommandValidator() => RuleFor(x => x.AmenityId).GreaterThan(0);
}