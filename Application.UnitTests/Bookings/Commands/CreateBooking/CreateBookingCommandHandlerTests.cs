using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Bookings.Commands.CreateBooking;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private static readonly DateOnly CheckIn = DateOnly.FromDateTime(DateTime.UtcNow.Date);
    private static readonly DateOnly CheckOut = CheckIn.AddDays(3);

    private CreateBookingCommandHandler CreateHandler() =>
        new(_rooms.Object, _bookings.Object, _uow.Object, _currentUser.Object);

    private static CreateBookingCommand Command() =>
        new(RoomId: 1, CheckIn, CheckOut, Adults: 2, Children: 0, SpecialRequests: null);

    private static Room BookableRoom() =>
        Room.Create(hotelId: 1, "101", RoomType.Standard, 2, 1, Money.Of(100m));

    [Fact]
    public async Task Handle_AvailableRoom_CreatesBookingWithComputedTotal()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _rooms.Setup(r => r.GetByIdForBookingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(BookableRoom());
        _rooms.Setup(r => r.IsAvailableAsync(It.IsAny<int>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.TotalPrice.Should().Be(300m); // 100 * 3 nights
        result.Status.Should().Be(BookingStatus.Pending.ToString());
        result.ConfirmationNumber.Should().StartWith("HB-");
        _bookings.Verify(b => b.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _rooms.Verify(r => r.Update(It.IsAny<Room>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotLoggedIn_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _rooms.Setup(r => r.GetByIdForBookingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Lightweight guard for the double-booking race: when two reservations pass the availability
    // check concurrently, the Room rowversion makes the loser's SaveChanges fail, which the DbContext
    // translates to ConflictException (-> 409). This asserts the handler builds the booking and lets
    // that conflict surface rather than swallowing it. The real two-parallel-requests proof belongs
    // in an integration test (Testcontainers SQL Server) and is intentionally not covered here.
    [Fact]
    public async Task Handle_ConcurrentReservationLosesRace_SurfacesConflict()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _rooms.Setup(r => r.GetByIdForBookingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(BookableRoom());
        _rooms.Setup(r => r.IsAvailableAsync(It.IsAny<int>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("The resource was modified by another request. Please retry."));

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _bookings.Verify(b => b.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _rooms.Verify(r => r.Update(It.IsAny<Room>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoomUnavailable_ThrowsRoomNotAvailable_AndDoesNotPersist()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _rooms.Setup(r => r.GetByIdForBookingAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(BookableRoom());
        _rooms.Setup(r => r.IsAvailableAsync(It.IsAny<int>(), It.IsAny<DateRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<RoomNotAvailableException>();
        _bookings.Verify(b => b.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
