using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using FluentValidation;

namespace Application.Features.Reviews.Commands.CreateReview;

public class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    private readonly IHotelRepository _hotelRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ICurrentUserService _currentUserService;

    public CreateReviewCommandValidator(
        IHotelRepository hotelRepository, IReviewRepository reviewRepository,
        IBookingRepository bookingRepository, ICurrentUserService currentUserService)
    {
        _hotelRepository = hotelRepository;
        _reviewRepository = reviewRepository;
        _bookingRepository = bookingRepository;
        _currentUserService = currentUserService;

        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(2000);

        RuleFor(x => x.HotelId)
            .MustAsync(HotelExists).WithMessage("The specified hotel does not exist.")
            .MustAsync(NotAlreadyReviewed).WithMessage("You have already reviewed this hotel.")
            .MustAsync(HaveCompletedStay).WithMessage("You can only review a hotel after completing a stay there.");
    }

    private async Task<bool> HotelExists(int hotelId, CancellationToken cancellationToken) =>
        await _hotelRepository.GetByIdAsync(hotelId, cancellationToken) is not null;

    private async Task<bool> NotAlreadyReviewed(int hotelId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        return !await _reviewRepository.UserHasReviewedAsync(userId, hotelId, cancellationToken);
    }

    private async Task<bool> HaveCompletedStay(int hotelId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        return await _bookingRepository.HasCompletedStayAsync(userId, hotelId, cancellationToken);
    }
}