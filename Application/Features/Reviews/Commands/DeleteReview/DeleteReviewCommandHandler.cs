using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException(nameof(Review), request.ReviewId);
        
        var isAuthor = review.UserId == currentUserService.UserId;
        var isAdmin = currentUserService.IsInRole("Admin");

        if (!isAuthor && !isAdmin)
            throw new ForbiddenAccessException("Only the review's author or an Admin can delete this review.");

        reviewRepository.Remove(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}