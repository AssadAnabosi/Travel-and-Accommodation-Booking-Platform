using FluentValidation;

namespace Application.Features.Hotels.Queries.GetHotels;

public class GetHotelsQueryValidator : AbstractValidator<GetHotelsQuery>
{
    public GetHotelsQueryValidator()
    {
        RuleFor(x => x.Keyword).MaximumLength(100);
        RuleFor(x => x.CityId).GreaterThan(0).When(x => x.CityId.HasValue);
        RuleFor(x => x.ApprovalStatus).IsInEnum().When(x => x.ApprovalStatus.HasValue);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
