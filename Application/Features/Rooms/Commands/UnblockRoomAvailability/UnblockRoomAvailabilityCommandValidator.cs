using FluentValidation;

namespace Application.Features.Rooms.Commands.UnblockRoomAvailability;

public class UnblockRoomAvailabilityCommandValidator : AbstractValidator<UnblockRoomAvailabilityCommand>
{
    public UnblockRoomAvailabilityCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.AvailabilityId).GreaterThan(0);
    }
}