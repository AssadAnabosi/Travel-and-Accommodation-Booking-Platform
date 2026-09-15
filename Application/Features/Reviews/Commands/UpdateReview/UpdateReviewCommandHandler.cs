using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Reviews.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandler(
    IReviewRepository reviewRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await reviewRepository.GetByIdAsync(request.ReviewId, cancellationToken)
                     ?? throw new NotFoundException(nameof(Review), request.ReviewId);

        if (review.UserId != currentUserService.UserId)
            throw new ForbiddenAccessException("You can only edit your own review.");

        review.Edit(request.Rating, request.Comment);

        reviewRepository.Update(review);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(review.UserId, cancellationToken);

        return new ReviewDto(review.Id, review.HotelId, review.UserId, $"{user!.FirstName} {user.LastName}",
            review.Rating, review.Comment, review.CreatedAt, review.ModifiedAt);
    }
}