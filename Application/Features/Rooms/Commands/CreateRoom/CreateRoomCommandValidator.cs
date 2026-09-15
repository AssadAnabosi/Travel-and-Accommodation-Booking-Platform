using Application.Common.Interfaces.Persistence;
using FluentValidation;

namespace Application.Features.Rooms.Commands.CreateRoom;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IRoomRepository _roomRepository;

    public CreateRoomCommandValidator(IHotelRepository hotelRepository, IRoomRepository roomRepository)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;

        RuleFor(x => x.HotelId).MustAsync(HotelExists).WithMessage("The specified hotel does not exist.");
        RuleFor(x => x.RoomType).IsInEnum();
        RuleFor(x => x.AdultCapacity).GreaterThan(0);
        RuleFor(x => x.ChildCapacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BasePrice).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);

        RuleFor(x => x.Number)
            .NotEmpty().MaximumLength(20)
            .MustAsync(BeUniqueInHotel).WithMessage("This room number already exists for this hotel.");
    }

    private async Task<bool> HotelExists(int hotelId, CancellationToken cancellationToken) =>
        await _hotelRepository.GetByIdAsync(hotelId, cancellationToken) is not null;

    private async Task<bool> BeUniqueInHotel(CreateRoomCommand command, string number,
        CancellationToken cancellationToken)
    {
        var exists =
            await _roomRepository.NumberExistsInHotelAsync(command.HotelId, number, excludeId: null, cancellationToken);
        return !exists;
    }
}