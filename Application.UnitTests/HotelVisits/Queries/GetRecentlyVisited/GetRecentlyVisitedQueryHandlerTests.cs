using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.HotelVisits.Queries.GetRecentlyVisited;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.HotelVisits.Queries.GetRecentlyVisited;

public class GetRecentlyVisitedQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 7, 1);

    private readonly Mock<IHotelVisitRepository> _visits = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Guid _userId = Guid.NewGuid();

    public GetRecentlyVisitedQueryHandlerTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(_userId);
        _clock.Setup(c => c.Today).Returns(Today);
    }

    private GetRecentlyVisitedQueryHandler CreateHandler() => new(_visits.Object, _currentUser.Object, _clock.Object);

    [Fact]
    public async Task Handle_MapsThumbnailAndCheapestActiveRoomPriceAfterDiscounts()
    {
        var hotel = TestData.Hotel(Guid.NewGuid(), name: "Seaside", id: 5, cityName: "Nice")
            .WithItems("_images", HotelImage.Create(5, "second.jpg", 1), HotelImage.Create(5, "first.jpg", 0));
        var discounted = TestData.RoomIn(hotel, basePrice: 200m, id: 1);
        TestData.DiscountOn(discounted, DiscountType.Percentage, 50m, Today.AddDays(-1), Today.AddDays(5)); // → 100
        TestData.RoomIn(hotel, basePrice: 120m, id: 2);
        TestData.RoomIn(hotel, basePrice: 10m, id: 3).Retire(); // inactive rooms never set the price
        _visits.Setup(v => v.GetRecentlyVisitedAsync(_userId, 3, It.IsAny<CancellationToken>())).ReturnsAsync([hotel]);

        var result = await CreateHandler().Handle(new GetRecentlyVisitedQuery(3), CancellationToken.None);

        var item = result.Should().ContainSingle().Subject;
        item.HotelId.Should().Be(5);
        item.Name.Should().Be("Seaside");
        item.CityName.Should().Be("Nice");
        item.ThumbnailUrl.Should().Be("first.jpg");
        item.PricePerNight.Should().Be(100m);
        item.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_HotelWithoutActiveRoomsOrImages_FallsBackToZeroUsdAndNoThumbnail()
    {
        var hotel = TestData.Hotel(Guid.NewGuid());
        _visits.Setup(v => v.GetRecentlyVisitedAsync(_userId, 5, It.IsAny<CancellationToken>())).ReturnsAsync([hotel]);

        var result = await CreateHandler().Handle(new GetRecentlyVisitedQuery(), CancellationToken.None);

        var item = result.Should().ContainSingle().Subject;
        item.ThumbnailUrl.Should().BeNull();
        item.PricePerNight.Should().Be(0m);
        item.Currency.Should().Be("USD");
    }
}
