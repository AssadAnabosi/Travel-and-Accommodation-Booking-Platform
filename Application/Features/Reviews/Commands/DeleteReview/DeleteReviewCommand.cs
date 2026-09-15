using Application.Common.Security;
using MediatR;

namespace Application.Features.Reviews.Commands.DeleteReview;

[Authorize]
public record DeleteReviewCommand(int ReviewId) : IRequest;