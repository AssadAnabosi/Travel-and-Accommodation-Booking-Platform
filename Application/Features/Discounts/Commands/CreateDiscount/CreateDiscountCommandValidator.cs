using Application.Common.Interfaces.Persistence;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Discounts.Commands.CreateDiscount;

public class CreateDiscountCommandValidator : AbstractValidator<CreateDiscountCommand>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IDiscountRepository _discountRepository;

    public CreateDiscountCommandValidator(IRoomRepository roomRepository, IDiscountRepository discountRepository)
    {
        _roomRepository = roomRepository;
        _discountRepository = discountRepository;

        RuleFor(x => x.RoomId).MustAsync(RoomExists).WithMessage("The specified room does not exist.");
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
            .WithMessage("This room already has an active discount overlapping these dates.")
            .OverridePropertyName(nameof(CreateDiscountCommand.StartDate));
    }

    private async Task<bool> RoomExists(int roomId, CancellationToken cancellationToken) =>
        await _roomRepository.GetByIdAsync(roomId, cancellationToken) is not null;

    private async Task<bool> NotOverlapExistingDiscount(CreateDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var overlaps = await _discountRepository.HasOverlappingDiscountAsync(
            command.RoomId, command.StartDate, command.EndDate, excludeId: null, cancellationToken);
        return !overlaps;
    }
}