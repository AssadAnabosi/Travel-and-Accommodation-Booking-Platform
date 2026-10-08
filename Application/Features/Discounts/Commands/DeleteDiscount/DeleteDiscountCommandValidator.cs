using FluentValidation;

namespace Application.Features.Discounts.Commands.DeleteDiscount;

public class DeleteDiscountCommandValidator : AbstractValidator<DeleteDiscountCommand>
{
    public DeleteDiscountCommandValidator() => RuleFor(x => x.DiscountId).GreaterThan(0);
}