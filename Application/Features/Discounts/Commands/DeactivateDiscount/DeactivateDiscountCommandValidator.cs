using FluentValidation;

namespace Application.Features.Discounts.Commands.DeactivateDiscount;

public class DeactivateDiscountCommandValidator : AbstractValidator<DeactivateDiscountCommand>
{
    public DeactivateDiscountCommandValidator() => RuleFor(x => x.DiscountId).GreaterThan(0);
}