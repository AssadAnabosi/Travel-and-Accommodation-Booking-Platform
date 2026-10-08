using Domain.Entities;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class ReviewTests
{
    [Fact]
    public void Create_SetsProperties()
    {
        var userId = Guid.NewGuid();

        var review = Review.Create(hotelId: 1, userId, rating: 4, comment: "Lovely stay");

        review.HotelId.Should().Be(1);
        review.UserId.Should().Be(userId);
        review.Rating.Should().Be(4);
        review.Comment.Should().Be("Lovely stay");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Create_RatingOutOfRange_Throws(int rating)
    {
        var act = () => Review.Create(1, Guid.NewGuid(), rating, null);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Edit_UpdatesRatingAndComment()
    {
        var review = Review.Create(1, Guid.NewGuid(), 3, "ok");

        review.Edit(5, "Actually great");

        review.Rating.Should().Be(5);
        review.Comment.Should().Be("Actually great");
    }

    [Fact]
    public void Edit_InvalidRating_Throws()
    {
        var review = Review.Create(1, Guid.NewGuid(), 3, "ok");

        var act = () => review.Edit(9, "bad");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}