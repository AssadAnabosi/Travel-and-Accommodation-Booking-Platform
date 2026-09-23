using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Queries.GetFeaturedDeals;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetFeaturedDeals;

public class GetFeaturedDealsQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 7, 1);

    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public GetFeaturedDealsQueryHandlerTests() => _clock.Setup(c => c.Today).Returns(Today);

    private GetFeaturedDealsQueryHandler CreateHandler() => new(_hotels.Object, _clock.Object);

    [Fact]
    public async Task Handle_PicksTheCheapestDiscountedActiveRoomPerHotel()
    {
        var hotel = TestData.Hotel(Guid.NewGuid(), name: "Grand", id: 1, cityName: "Paris")
            .WithItems("_images", HotelImage.Create(1, "b.jpg", 1), HotelImage.Create(1, "a.jpg", 0));
        var pct = TestData.RoomIn(hotel, basePrice: 200m, id: 1);
        TestData.DiscountOn(pct, DiscountType.Percentage, 10m, Today, Today.AddDays(3)); // 180
        var fixedOff = TestData.RoomIn(hotel, basePrice: 150m, id: 2);
        TestData.DiscountOn(fixedOff, DiscountType.FixedAmount, 20m, Today, Today.AddDays(3)); // 130
        TestData.RoomIn(hotel, basePrice: 90m, id: 3); // cheaper, but not discounted → not a deal
        var retired = TestData.RoomIn(hotel, basePrice: 100m, id: 4);
        TestData.DiscountOn(retired, DiscountType.Percentage, 90m, Today, Today.AddDays(3)); // 10, but inactive
        retired.Retire();
        _hotels.Setup(h => h.GetFeaturedDealsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync([hotel]);

        var deals = await CreateHandler().Handle(new GetFeaturedDealsQuery(), CancellationToken.None);

        var deal = deals.Should().ContainSingle().Subject;
        deal.HotelId.Should().Be(1);
        deal.Name.Should().Be("Grand");
        deal.CityName.Should().Be("Paris");
        deal.ThumbnailUrl.Should().Be("a.jpg");
        deal.OriginalPrice.Should().Be(150m);
        deal.DiscountedPrice.Should().Be(130m);
        deal.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_SkipsHotelsWhoseDiscountsAreNotActiveToday()
    {
        var expired = TestData.Hotel(Guid.NewGuid(), id: 1);
        TestData.DiscountOn(TestData.RoomIn(expired), DiscountType.Percentage, 10m,
            Today.AddDays(-10), Today.AddDays(-1));
        var current = TestData.Hotel(Guid.NewGuid(), name: "Current", id: 2);
        TestData.DiscountOn(TestData.RoomIn(current), DiscountType.Percentage, 10m, Today, Today.AddDays(1));
        _hotels.Setup(h => h.GetFeaturedDealsAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync([expired, current]);

        var deals = await CreateHandler().Handle(new GetFeaturedDealsQuery(), CancellationToken.None);

        deals.Should().ContainSingle().Which.Name.Should().Be("Current");
    }
}
