using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Reviews.Queries.GetReviewsByHotel;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Reviews.Queries.GetReviewsByHotel;

public class GetReviewsByHotelQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IReviewRepository> _reviews = new();

    private GetReviewsByHotelQueryHandler CreateHandler() => new(_hotels.Object, _reviews.Object);

    [Fact]
    public async Task Handle_ExistingHotel_MapsReviewerNameAndPage()
    {
        var author = User.Create("ada@tabp.dev", "hash", "Ada", "Lovelace");
        var review = Review.Create(1, author.Id, 5, "Lovely").WithId(9).With(nameof(Review.User), author);
        _hotels.Setup(h => h.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(TestData.Hotel(Guid.NewGuid()));
        _reviews.Setup(r => r.GetByHotelIdAsync(1, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Review>([review], totalCount: 1, pageNumber: 1, pageSize: 20));

        var page = await CreateHandler().Handle(new GetReviewsByHotelQuery(1), CancellationToken.None);

        var dto = page.Items.Should().ContainSingle().Subject;
        dto.Id.Should().Be(9);
        dto.HotelId.Should().Be(1);
        dto.UserId.Should().Be(author.Id);
        dto.ReviewerName.Should().Be("Ada Lovelace");
        dto.Rating.Should().Be(5);
        dto.Comment.Should().Be("Lovely");
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFoundWithoutQueryingReviews()
    {
        _hotels.Setup(h => h.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new GetReviewsByHotelQuery(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _reviews.Verify(r => r.GetByHotelIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
