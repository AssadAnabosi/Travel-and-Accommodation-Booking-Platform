using FluentValidation;

namespace Application.Features.Hotels.Commands.DeleteHotel;

public class DeleteHotelCommandValidator : AbstractValidator<DeleteHotelCommand>
{
    public DeleteHotelCommandValidator() => RuleFor(x => x.HotelId).GreaterThan(0);
}