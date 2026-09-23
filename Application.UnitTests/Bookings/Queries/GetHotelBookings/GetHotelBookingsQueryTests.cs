using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Bookings.Queries.GetHotelBookings;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace Application.UnitTests.Bookings.Queries.GetHotelBookings;

public class GetHotelBookingsQueryTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private GetHotelBookingsQueryHandler CreateHandler() => new(_hotels.Object, _bookings.Object, _currentUser.Object);

    private Hotel GivenHotel()
    {
        var hotel = TestData.Hotel(_ownerId);
        _hotels.Setup(r => r.GetByIdAsync(hotel.Id, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        return hotel;
    }

    [Fact]
    public async Task Handle_Owner_PassesFilterAndMapsGuestRoomAndStay()
    {
        var hotel = GivenHotel();
        var guest = User.Create("ada@example.com", "hash", "Ada", "Lovelace");
        var booking = TestData.BookingFor(guest, TestData.RoomIn(hotel, number: "204"), confirmed: true);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);
        _bookings.Setup(b => b.GetByHotelIdAsync(hotel.Id, It.IsAny<HotelBookingFilter>(), 2, 10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Booking>([booking], 11, 2, 10));
        var today = new DateOnly(2026, 10, 1);

        var result = await CreateHandler().Handle(
            new GetHotelBookingsQuery(hotel.Id, BookingStatus.Confirmed, today, today, "ada", PageNumber: 2, PageSize: 10),
            CancellationToken.None);

        _bookings.Verify(b => b.GetByHotelIdAsync(hotel.Id,
            new HotelBookingFilter(BookingStatus.Confirmed, today, today, "ada"), 2, 10,
            It.IsAny<CancellationToken>()), Times.Once);
        result.TotalCount.Should().Be(11);
        var row = result.Items.Single();
        row.GuestName.Should().Be("Ada Lovelace");
        row.GuestEmail.Should().Be("ada@example.com");
        row.RoomNumber.Should().Be("204");
        row.CheckIn.Should().Be(new DateOnly(2026, 10, 1));
        row.CheckOut.Should().Be(new DateOnly(2026, 10, 4));
        row.Status.Should().Be("Confirmed");
        row.Adults.Should().Be(2);
        row.SpecialRequests.Should().Be("Quiet room");
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbidden()
    {
        var hotel = GivenHotel();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetHotelBookingsQuery(hotel.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _bookings.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UnknownHotel_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var act = () => CreateHandler().Handle(new GetHotelBookingsQuery(99), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Validator_ValidQuery_HasNoErrors()
    {
        new GetHotelBookingsQueryValidator()
            .TestValidate(new GetHotelBookingsQuery(1, BookingStatus.CheckedIn, new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1)))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_ToBeforeFrom_HasError()
    {
        new GetHotelBookingsQueryValidator()
            .TestValidate(new GetHotelBookingsQuery(1, CheckInFrom: new DateOnly(2026, 1, 2),
                CheckInTo: new DateOnly(2026, 1, 1)))
            .ShouldHaveValidationErrorFor(x => x.CheckInTo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validator_PageSizeOutOfRange_HasError(int pageSize)
    {
        new GetHotelBookingsQueryValidator()
            .TestValidate(new GetHotelBookingsQuery(1, PageSize: pageSize))
            .ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validator_UndefinedStatus_HasError()
    {
        new GetHotelBookingsQueryValidator()
            .TestValidate(new GetHotelBookingsQuery(1, (BookingStatus)42))
            .ShouldHaveValidationErrorFor(x => x.Status);
    }
}
