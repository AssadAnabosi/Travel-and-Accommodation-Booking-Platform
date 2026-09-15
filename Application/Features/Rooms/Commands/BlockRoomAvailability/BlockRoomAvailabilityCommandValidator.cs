using FluentValidation;

namespace Application.Features.Rooms.Commands.BlockRoomAvailability;

public class BlockRoomAvailabilityCommandValidator : AbstractValidator<BlockRoomAvailabilityCommand>
{
    public BlockRoomAvailabilityCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
    }
}