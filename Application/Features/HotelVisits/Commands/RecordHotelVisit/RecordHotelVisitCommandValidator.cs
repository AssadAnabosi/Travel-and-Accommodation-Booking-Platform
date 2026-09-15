using FluentValidation;

namespace Application.Features.HotelVisits.Commands.RecordHotelVisit;

public class RecordHotelVisitCommandValidator : AbstractValidator<RecordHotelVisitCommand>
{
    public RecordHotelVisitCommandValidator() => RuleFor(x => x.HotelId).GreaterThan(0);
}