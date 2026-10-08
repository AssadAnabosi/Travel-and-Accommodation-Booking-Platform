using FluentValidation;

namespace Application.Features.Cities.Commands.DeleteCity;

public class DeleteCityCommandValidator : AbstractValidator<DeleteCityCommand>
{
    public DeleteCityCommandValidator()
    {
        RuleFor(x => x.CityId).GreaterThan(0);
    }
}