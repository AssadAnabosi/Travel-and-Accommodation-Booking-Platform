using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Reviews.Commands.UpdateReview;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly User _author = User.Create("ada@tabp.dev", "hash", "Ada", "Lovelace");
    private readonly Review _review;

    public UpdateReviewCommandHandlerTests()
    {
        _review = Review.Create(1, _author.Id, 3, "Okay").WithId(9);
        _reviews.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(_review);
        _users.Setup(u => u.GetByIdAsync(_author.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_author);
    }

    private UpdateReviewCommandHandler CreateHandler() =>
        new(_reviews.Object, _users.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Author_EditsAndReturnsDtoWithReviewerName()
    {
        _currentUser.Setup(c => c.UserId).Returns(_author.Id);

        var dto = await CreateHandler().Handle(new UpdateReviewCommand(9, 5, "Great after all"),
            CancellationToken.None);

        _review.Rating.Should().Be(5);
        dto.Rating.Should().Be(5);
        dto.Comment.Should().Be("Great after all");
        dto.ReviewerName.Should().Be("Ada Lovelace");
        dto.ModifiedAt.Should().NotBeNull();
        _reviews.Verify(r => r.Update(_review), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminWhoIsNotTheAuthor_ThrowsForbidden()
    {
        // Only the author edits; Admin may delete but not rewrite someone's review.
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new UpdateReviewCommand(9, 1, "Edited"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _review.Rating.Should().Be(3);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReviewNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new UpdateReviewCommand(99, 5, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
