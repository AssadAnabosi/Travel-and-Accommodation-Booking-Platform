using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Bookings.Commands.CheckInBooking;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Commands.CheckInBooking;

public class CheckInBookingCommandHandlerTests
{
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _hotelOwnerId = Guid.NewGuid();

    private CheckInBookingCommandHandler CreateHandler() => new(_bookings.Object, _uow.Object, _currentUser.Object);

    private Booking GivenBooking(bool confirmed)
    {
        var guest = User.Create("g@tabp.dev", "hash", "Gina", "Guest");
        var booking = TestData.BookingFor(guest, TestData.RoomIn(TestData.Hotel(_hotelOwnerId)), confirmed);
        _bookings.Setup(b => b.GetByIdWithDetailsAsync(booking.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(booking);
        return booking;
    }

    [Fact]
    public async Task Handle_HotelOwner_ChecksInConfirmedBooking()
    {
        var booking = GivenBooking(confirmed: true);
        _currentUser.Setup(c => c.UserId).Returns(_hotelOwnerId);

        await CreateHandler().Handle(new CheckInBookingCommand(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CheckedIn);
        _bookings.Verify(b => b.Update(booking), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        var booking = GivenBooking(confirmed: true);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await CreateHandler().Handle(new CheckInBookingCommand(booking.Id), CancellationToken.None);

        booking.Status.Should().Be(BookingStatus.CheckedIn);
    }

    [Fact]
    public async Task Handle_PendingBooking_ThrowsInvalidStateTransition()
    {
        // Mapped to 409 by the global handler (decision #68).
        var booking = GivenBooking(confirmed: false);
        _currentUser.Setup(c => c.UserId).Returns(_hotelOwnerId);

        var act = () => CreateHandler().Handle(new CheckInBookingCommand(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStateTransitionException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnerOfAnotherHotel_ThrowsForbidden()
    {
        var booking = GivenBooking(confirmed: true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new CheckInBookingCommand(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        booking.Status.Should().Be(BookingStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_BookingNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new CheckInBookingCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
