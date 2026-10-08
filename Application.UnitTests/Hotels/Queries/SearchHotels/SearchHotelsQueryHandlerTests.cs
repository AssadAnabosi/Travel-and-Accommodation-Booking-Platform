using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Hotels.Queries.SearchHotels;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.SearchHotels;

public class SearchHotelsQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 7, 1);

    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public SearchHotelsQueryHandlerTests() => _clock.Setup(c => c.Today).Returns(Today);

    private SearchHotelsQueryHandler CreateHandler() => new(_hotels.Object, _clock.Object);

    [Fact]
    public async Task Handle_PassesEveryCriterionToTheRepositoryFilter()
    {
        HotelSearchFilter? filter = null;
        _hotels.Setup(h => h.SearchAsync(It.IsAny<HotelSearchFilter>(), 2, 10, It.IsAny<CancellationToken>()))
            .Callback<HotelSearchFilter, int, int, CancellationToken>((f, _, _, _) => filter = f)
            .ReturnsAsync(new PaginatedList<Hotel>([], 0, 2, 10));
        var query = new SearchHotelsQuery("sea", 3, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 4),
            Adults: 3, Children: 1, Rooms: 2, MinPrice: 50m, MaxPrice: 300m, MinStarRating: 4,
            AmenityIds: [1, 2], RoomType: RoomType.Suite, PageNumber: 2, PageSize: 10);

        await CreateHandler().Handle(query, CancellationToken.None);

        filter.Should().BeEquivalentTo(new HotelSearchFilter("sea", 3, new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 4), 3, 1, 2, 50m, 300m, 4, [1, 2], RoomType.Suite));
    }

    [Fact]
    public async Task Handle_MapsCheapestActivePriceThumbnailAndTruncatedDescription()
    {
        var hotel = TestData.Hotel(Guid.NewGuid(), name: "Seaside", id: 5, cityName: "Nice")
            .With(nameof(Hotel.Description), new string('x', 200))
            .WithItems("_images", HotelImage.Create(5, "second.jpg", 1), HotelImage.Create(5, "first.jpg", 0));
        var discounted = TestData.RoomIn(hotel, basePrice: 180m, id: 1);
        TestData.DiscountOn(discounted, DiscountType.FixedAmount, 100m, Today, Today.AddDays(2)); // 80
        TestData.RoomIn(hotel, basePrice: 90m, id: 2);
        TestData.RoomIn(hotel, basePrice: 20m, id: 3).Retire();
        var plain = TestData.Hotel(Guid.NewGuid(), name: "Plain", id: 6); // "A nice place", no rooms
        _hotels.Setup(h => h.SearchAsync(It.IsAny<HotelSearchFilter>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Hotel>([hotel, plain], 2, 1, 20));

        var page = await CreateHandler().Handle(new SearchHotelsQuery(null, null, null, null), CancellationToken.None);

        var seaside = page.Items[0];
        seaside.HotelId.Should().Be(5);
        seaside.CityName.Should().Be("Nice");
        seaside.ThumbnailUrl.Should().Be("first.jpg");
        seaside.PricePerNight.Should().Be(80m);
        seaside.ShortDescription.Should().Be(new string('x', 150) + "...");
        var noRooms = page.Items[1];
        noRooms.ShortDescription.Should().Be("A nice place");
        noRooms.PricePerNight.Should().Be(0m);
        noRooms.Currency.Should().Be("USD");
        noRooms.ThumbnailUrl.Should().BeNull();
    }
}
