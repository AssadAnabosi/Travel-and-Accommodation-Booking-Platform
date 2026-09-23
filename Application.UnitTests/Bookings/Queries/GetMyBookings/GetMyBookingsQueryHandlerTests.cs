using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Bookings.Queries.GetMyBookings;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Queries.GetMyBookings;

public class GetMyBookingsQueryHandlerTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_QueriesCurrentUsersBookingsAndMapsPage()
    {
        var guest = User.Create("guest@tabp.dev", "hash", "Gina", "Guest");
        _currentUser.Setup(c => c.UserId).Returns(guest.Id);
        var booking = TestData.BookingFor(guest,
            TestData.RoomIn(TestData.Hotel(Guid.NewGuid(), name: "Grand"), number: "101"), confirmed: true);
        _bookings.Setup(b => b.GetByUserIdAsync(guest.Id, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Booking>([booking], totalCount: 11, pageNumber: 2, pageSize: 10));

        var page = await new GetMyBookingsQueryHandler(_bookings.Object, _currentUser.Object)
            .Handle(new GetMyBookingsQuery(PageNumber: 2, PageSize: 10), CancellationToken.None);

        page.PageNumber.Should().Be(2);
        page.TotalCount.Should().Be(11);
        page.TotalPages.Should().Be(2);
        var item = page.Items.Should().ContainSingle().Subject;
        item.Id.Should().Be(booking.Id);
        item.HotelName.Should().Be("Grand");
        item.RoomNumber.Should().Be("101");
        item.Status.Should().Be("Confirmed");
        item.CheckIn.Should().Be(new DateOnly(2026, 10, 1));
        item.CheckOut.Should().Be(new DateOnly(2026, 10, 4));
        item.TotalPrice.Should().Be(300m);
        item.Currency.Should().Be("USD");
    }
}
