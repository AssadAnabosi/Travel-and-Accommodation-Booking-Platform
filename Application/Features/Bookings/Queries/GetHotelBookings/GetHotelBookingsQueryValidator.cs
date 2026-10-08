using FluentValidation;

namespace Application.Features.Bookings.Queries.GetHotelBookings;

public class GetHotelBookingsQueryValidator : AbstractValidator<GetHotelBookingsQuery>
{
    public GetHotelBookingsQueryValidator()
    {
        RuleFor(x => x.HotelId).GreaterThan(0);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.CheckInTo)
            .GreaterThanOrEqualTo(x => x.CheckInFrom)
            .When(x => x.CheckInFrom.HasValue && x.CheckInTo.HasValue)
            .WithMessage("CheckInTo must be on or after CheckInFrom.");
        RuleFor(x => x.Keyword).MaximumLength(100);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
