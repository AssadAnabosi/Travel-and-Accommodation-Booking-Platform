using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Reviews.Commands.CreateReview;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private readonly Guid _userId = Guid.NewGuid();

    public CreateReviewCommandHandlerTests() => _currentUser.Setup(c => c.UserId).Returns(_userId);

    private CreateReviewCommandHandler CreateHandler() =>
        new(_reviews.Object, _bookings.Object, _users.Object, _uow.Object, _currentUser.Object);

    private static CreateReviewCommand Command() => new(HotelId: 1, Rating: 5, Comment: "Great stay");

    [Fact]
    public async Task Handle_VerifiedStay_FirstReview_CreatesReview()
    {
        _bookings.Setup(b => b.HasCompletedStayAsync(_userId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _reviews.Setup(r => r.UserHasReviewedAsync(_userId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.GetByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create("ada@example.com", "hash", "Ada", "Lovelace"));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Rating.Should().Be(5);
        result.Comment.Should().Be("Great stay");
        result.ReviewerName.Should().Be("Ada Lovelace");
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoCompletedStay_ThrowsForbidden_AndDoesNotPersist()
    {
        _bookings.Setup(b => b.HasCompletedStayAsync(_userId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ThrowsConflict()
    {
        _bookings.Setup(b => b.HasCompletedStayAsync(_userId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _reviews.Setup(r => r.UserHasReviewedAsync(_userId, 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
