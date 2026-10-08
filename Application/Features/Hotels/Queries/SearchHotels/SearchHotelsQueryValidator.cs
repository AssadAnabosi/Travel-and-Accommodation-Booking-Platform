using FluentValidation;

namespace Application.Features.Hotels.Queries.SearchHotels;

public class SearchHotelsQueryValidator : AbstractValidator<SearchHotelsQuery>
{
    public SearchHotelsQueryValidator()
    {
        RuleFor(x => x.Adults).GreaterThan(0);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Rooms).GreaterThan(0);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("Check-out date must be after check-in date.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be greater than or equal to MinPrice.");
    }
}