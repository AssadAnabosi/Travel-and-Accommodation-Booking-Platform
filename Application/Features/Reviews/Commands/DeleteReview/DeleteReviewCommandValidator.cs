using FluentValidation;

namespace Application.Features.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator() => RuleFor(x => x.ReviewId).GreaterThan(0);
}