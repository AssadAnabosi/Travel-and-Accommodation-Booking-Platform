using FluentValidation;

namespace Application.Features.Rooms.Commands.DeleteRoom;

public class DeleteRoomCommandValidator : AbstractValidator<DeleteRoomCommand>
{
    public DeleteRoomCommandValidator() => RuleFor(x => x.RoomId).GreaterThan(0);
}