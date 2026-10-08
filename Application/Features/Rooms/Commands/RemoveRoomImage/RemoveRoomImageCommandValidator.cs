using FluentValidation;

namespace Application.Features.Rooms.Commands.RemoveRoomImage;

public class RemoveRoomImageCommandValidator : AbstractValidator<RemoveRoomImageCommand>
{
    public RemoveRoomImageCommandValidator()
    {
        RuleFor(x => x.RoomId).GreaterThan(0);
        RuleFor(x => x.ImageId).GreaterThan(0);
    }
}
