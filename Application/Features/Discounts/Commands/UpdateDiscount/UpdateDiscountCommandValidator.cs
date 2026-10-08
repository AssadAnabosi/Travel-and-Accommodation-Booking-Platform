using Application.Common.Interfaces.Persistence;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Discounts.Commands.UpdateDiscount;

public class UpdateDiscountCommandValidator : AbstractValidator<UpdateDiscountCommand>
{
    private readonly IDiscountRepository _discountRepository;

    public UpdateDiscountCommandValidator(IDiscountRepository discountRepository)
    {
        _discountRepository = discountRepository;

        RuleFor(x => x.DiscountId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).GreaterThan(0);

        RuleFor(x => x.Value)
            .LessThanOrEqualTo(100)
            .When(x => x.Type == DiscountType.Percentage)
            .WithMessage("Percentage discount cannot exceed 100.");

        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");

        RuleFor(x => x)
            .MustAsync(NotOverlapExistingDiscount)
            .WithMessage("This room already has another active discount overlapping these dates.")
            .OverridePropertyName(nameof(UpdateDiscountCommand.StartDate));
    }

    private async Task<bool> NotOverlapExistingDiscount(UpdateDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var existing = await _discountRepository.GetByIdWithRoomAsync(command.DiscountId, cancellationToken);
        if (existing is null) return true; // NotFoundException raised later in the handler

        var overlaps = await _discountRepository.HasOverlappingDiscountAsync(
            existing.RoomId, command.StartDate, command.EndDate, command.DiscountId, cancellationToken);
        return !overlaps;
    }
}