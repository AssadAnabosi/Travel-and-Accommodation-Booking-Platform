using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Reviews.Commands.DeleteReview;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandHandlerTests
{
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _authorId = Guid.NewGuid();
    private readonly Review _review;

    public DeleteReviewCommandHandlerTests()
    {
        _review = Review.Create(1, _authorId, 2, "Meh").WithId(9);
        _reviews.Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(_review);
    }

    private DeleteReviewCommandHandler CreateHandler() => new(_reviews.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Author_RemovesAndSaves()
    {
        _currentUser.Setup(c => c.UserId).Returns(_authorId);

        await CreateHandler().Handle(new DeleteReviewCommand(9), CancellationToken.None);

        _reviews.Verify(r => r.Remove(_review), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_CanDeleteAnyReview()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await CreateHandler().Handle(new DeleteReviewCommand(9), CancellationToken.None);

        _reviews.Verify(r => r.Remove(_review), Times.Once);
    }

    [Fact]
    public async Task Handle_HotelOwnerOrAnyoneElse_ThrowsForbidden()
    {
        // A HotelOwner can never delete reviews of their hotel.
        _currentUser.Setup(c => c.IsInRole("HotelOwner")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new DeleteReviewCommand(9), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _reviews.Verify(r => r.Remove(It.IsAny<Review>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReviewNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new DeleteReviewCommand(99), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
