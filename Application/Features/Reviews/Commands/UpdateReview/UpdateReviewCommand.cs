using Application.Common.Security;
using Application.Features.Reviews.Common;
using MediatR;

namespace Application.Features.Reviews.Commands.UpdateReview;

[Authorize]
public record UpdateReviewCommand(int ReviewId, int Rating, string? Comment) : IRequest<ReviewDto>;