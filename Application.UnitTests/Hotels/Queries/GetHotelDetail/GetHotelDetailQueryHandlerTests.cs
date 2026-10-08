using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Common;
using Application.Features.Hotels.Queries.GetHotelDetail;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetHotelDetail;

public class GetHotelDetailQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 7, 1);
    private static readonly DateOnly CheckIn = new(2026, 8, 10);
    private static readonly DateOnly CheckOut = new(2026, 8, 13);

    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IDateTimeProvider> _clock = new();

    public GetHotelDetailQueryHandlerTests() => _clock.Setup(c => c.Today).Returns(Today);

    private GetHotelDetailQueryHandler CreateHandler() => new(_hotels.Object, _clock.Object);

    private Hotel GivenApprovedHotel()
    {
        var author = User.Create("r@tabp.dev", "hash", "Rita", "Reviewer");
        var hotel = TestData.Hotel(Guid.NewGuid(), name: "Grand", id: 1, cityName: "Paris")
            .WithItems("_images", HotelImage.Create(1, "b.jpg", 1), HotelImage.Create(1, "a.jpg", 0))
            .WithItems("_reviews", Review.Create(1, author.Id, 5, null), Review.Create(1, author.Id, 4, null))
            .WithItems("_hotelAmenities",
                HotelAmenity.Create(1, 1).With(nameof(HotelAmenity.Amenity), Amenity.Create("Pool")),
                HotelAmenity.Create(1, 2).With(nameof(HotelAmenity.Amenity), Amenity.Create("Spa")));

        // Blocked for part of the stay → shown but unavailable.
        TestData.RoomIn(hotel, basePrice: 100m, id: 1).Block(DateRange.Of(CheckIn.AddDays(1), CheckIn.AddDays(2)));
        // Discount active on check-in only (not today) → priced at check-in.
        var discounted = TestData.RoomIn(hotel, basePrice: 200m, id: 2, type: RoomType.Suite)
            .WithItems("_images", RoomImage.Create(2, "suite.jpg", 0));
        TestData.DiscountOn(discounted, DiscountType.Percentage, 25m, CheckIn, CheckOut);
        TestData.RoomIn(hotel, basePrice: 50m, id: 3).Retire(); // never listed

        _hotels.Setup(h => h.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        return hotel;
    }

    [Fact]
    public async Task Handle_WithDates_MapsAvailabilityAndPricesAtCheckIn()
    {
        GivenApprovedHotel();

        var dto = await CreateHandler().Handle(new GetHotelDetailQuery(1, CheckIn, CheckOut), CancellationToken.None);

        dto.Name.Should().Be("Grand");
        dto.CityName.Should().Be("Paris");
        dto.AverageRating.Should().Be(4.5);
        dto.ReviewCount.Should().Be(2);
        dto.ImageUrls.Should().Equal("a.jpg", "b.jpg");
        dto.Amenities.Should().BeEquivalentTo("Pool", "Spa");
        dto.Rooms.Should().HaveCount(2);
        dto.Rooms[0].Should().Match<RoomSummaryDto>(r =>
            r.RoomId == 1 && !r.IsAvailable && r.PricePerNight == 100m);
        dto.Rooms[1].Should().Match<RoomSummaryDto>(r =>
            r.RoomId == 2 && r.IsAvailable && r.PricePerNight == 150m && r.RoomType == "Suite");
        dto.Rooms[1].ImageUrls.Should().Equal("suite.jpg");
    }

    [Fact]
    public async Task Handle_WithoutDates_EveryActiveRoomIsAvailableAndPricedToday()
    {
        GivenApprovedHotel();

        var dto = await CreateHandler().Handle(new GetHotelDetailQuery(1, null, null), CancellationToken.None);

        dto.Rooms.Should().OnlyContain(r => r.IsAvailable);
        dto.Rooms.Single(r => r.RoomId == 2).PricePerNight.Should().Be(200m); // discount starts later
    }

    [Fact]
    public async Task Handle_PendingHotel_ThrowsNotFoundSoItsExistenceIsNotRevealed()
    {
        _hotels.Setup(h => h.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Hotel(Guid.NewGuid(), approved: false));

        var act = () => CreateHandler().Handle(new GetHotelDetailQuery(1, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new GetHotelDetailQuery(99, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
