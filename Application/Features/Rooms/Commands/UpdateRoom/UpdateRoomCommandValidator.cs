using FluentValidation;

namespace Application.Features.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.AdultCapacity).GreaterThan(0);
        RuleFor(x => x.ChildCapacity).GreaterThanOrEqualTo(0);
    }
}