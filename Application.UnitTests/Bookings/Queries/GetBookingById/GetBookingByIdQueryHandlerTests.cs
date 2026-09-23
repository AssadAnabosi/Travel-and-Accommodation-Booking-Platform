using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Bookings.Queries.GetBookingById;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Queries.GetBookingById;

public class GetBookingByIdQueryHandlerTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _hotelOwnerId = Guid.NewGuid();
    private readonly User _guest = User.Create("guest@tabp.dev", "hash", "Gina", "Guest");
    private readonly Booking _booking;

    public GetBookingByIdQueryHandlerTests()
    {
        var room = TestData.RoomIn(TestData.Hotel(_hotelOwnerId, name: "Grand"), number: "204", type: RoomType.Suite);
        _booking = TestData.BookingFor(_guest, room);
        _bookings.Setup(b => b.GetByIdWithDetailsAsync(_booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_booking);
    }

    private GetBookingByIdQueryHandler CreateHandler() => new(_bookings.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Guest_ReturnsMappedDetail()
    {
        _currentUser.Setup(c => c.UserId).Returns(_guest.Id);

        var dto = await CreateHandler().Handle(new GetBookingByIdQuery(_booking.Id), CancellationToken.None);

        dto.Id.Should().Be(_booking.Id);
        dto.ConfirmationNumber.Should().Be(_booking.ConfirmationNumber);
        dto.Status.Should().Be("Pending");
        dto.HotelName.Should().Be("Grand");
        dto.HotelAddress.Should().Be("1 Main St");
        dto.RoomNumber.Should().Be("204");
        dto.RoomType.Should().Be("Suite");
        dto.CheckIn.Should().Be(new DateOnly(2026, 10, 1));
        dto.CheckOut.Should().Be(new DateOnly(2026, 10, 4));
        dto.Nights.Should().Be(3);
        dto.Adults.Should().Be(2);
        dto.Children.Should().Be(1);
        dto.TotalPrice.Should().Be(300m);
        dto.Currency.Should().Be("USD");
        dto.SpecialRequests.Should().Be("Quiet room");
    }

    [Fact]
    public async Task Handle_OwnerOfTheBookedHotel_IsAllowed()
    {
        _currentUser.Setup(c => c.UserId).Returns(_hotelOwnerId);

        var dto = await CreateHandler().Handle(new GetBookingByIdQuery(_booking.Id), CancellationToken.None);

        dto.Id.Should().Be(_booking.Id);
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var dto = await CreateHandler().Handle(new GetBookingByIdQuery(_booking.Id), CancellationToken.None);

        dto.Id.Should().Be(_booking.Id);
    }

    [Fact]
    public async Task Handle_UnrelatedUser_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetBookingByIdQuery(_booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new GetBookingByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
