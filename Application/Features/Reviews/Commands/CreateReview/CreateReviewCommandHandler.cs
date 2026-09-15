using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Reviews.Common;
using Domain.Entities;
using MediatR;

namespace Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler(
    IReviewRepository reviewRepository,
    IBookingRepository bookingRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId!.Value;

        // Re-checked here as defense-in-depth (same pattern as room-availability double-check
        // in CreateBooking) — closes the gap between validator and write under concurrency.
        if (!await bookingRepository.HasCompletedStayAsync(userId, request.HotelId, cancellationToken))
            throw new ForbiddenAccessException("You can only review a hotel after completing a stay there.");

        if (await reviewRepository.UserHasReviewedAsync(userId, request.HotelId, cancellationToken))
            throw new ConflictException("You have already reviewed this hotel.");

        var review = Review.Create(request.HotelId, userId, request.Rating, request.Comment);

        await reviewRepository.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);

        return new ReviewDto(review.Id, review.HotelId, review.UserId, $"{user!.FirstName} {user.LastName}",
            review.Rating, review.Comment, review.CreatedAt, review.ModifiedAt);
    }
}