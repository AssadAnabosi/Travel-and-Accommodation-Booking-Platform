using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Bookings.Commands.CheckOutBooking;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Commands.CheckOutBooking;

public class CheckOutBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _hotelOwnerId = Guid.NewGuid();

    private CheckOutBookingCommandHandler CreateHandler() => new(_bookings.Object, _uow.Object, _currentUser.Object);

    private Booking GivenBooking(bool checkedIn)
    {
        var guest = User.Create("g@tabp.dev", "hash", "Gina", "Guest");
        var booking = TestData.BookingFor(guest, TestData.RoomIn(TestData.Hotel(_hotelOwnerId)), confirmed: true);
        if (checkedIn) booking.CheckIn();
        _bookings.Setup(b => b.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task Handle_HotelOwner_ChecksOutCheckedInBooking()
    {
        var booking = GivenBooking(checkedIn: true);
        _currentUser.Setup(c => c.UserId).Returns(_hotelOwnerId);

        await CreateHandler().Handle(new CheckOutBookingCommand(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CheckedOut);
        _bookings.Verify(b => b.Update(booking), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        var booking = GivenBooking(checkedIn: true);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await CreateHandler().Handle(new CheckOutBookingCommand(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CheckedOut);
    }

    [Fact]
    public async Task Handle_NotYetCheckedIn_ThrowsInvalidStateTransition()
    {
        var booking = GivenBooking(checkedIn: false);
        _currentUser.Setup(c => c.UserId).Returns(_hotelOwnerId);

        var act = () => CreateHandler().Handle(new CheckOutBookingCommand(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStateTransitionException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnerOfAnotherHotel_ThrowsForbidden()
    {
        var booking = GivenBooking(checkedIn: true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new CheckOutBookingCommand(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        booking.Status.Should().Be(BookingStatus.CheckedIn);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new CheckOutBookingCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
