using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Commands.DeleteRoom;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Commands.DeleteRoom;

public class DeleteRoomCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private DeleteRoomCommandHandler CreateHandler() => new(_rooms.Object, _uow.Object, _currentUser.Object);

    // Admin so the handler skips the ownership check (which would need the Hotel navigation loaded).
    private void ActAsAdmin() => _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

    private Room GivenRoom()
    {
        var room = Room.Create(hotelId: 1, "101", RoomType.Standard, 2, 1, Money.Of(100m));
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(room);
        return room;
    }

    [Fact]
    public async Task Handle_RoomWithBookingHistory_SoftDeletes_AndDoesNotHardDelete()
    {
        ActAsAdmin();
        var room = GivenRoom();
        _rooms.Setup(r => r.HasAnyBookingsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await CreateHandler().Handle(new DeleteRoomCommand(1), CancellationToken.None);

        room.IsActive.Should().BeFalse();
        room.Number.Should().Contain("::deleted::");
        _rooms.Verify(r => r.Update(room), Times.Once);
        // Regression (decision #61): a booked room must never be hard-deleted (FK violation).
        _rooms.Verify(r => r.Remove(It.IsAny<Room>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoomNeverBooked_HardDeletes()
    {
        ActAsAdmin();
        var room = GivenRoom();
        _rooms.Setup(r => r.HasAnyBookingsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler().Handle(new DeleteRoomCommand(1), CancellationToken.None);

        _rooms.Verify(r => r.Remove(room), Times.Once);
        _rooms.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        ActAsAdmin();
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var act = () => CreateHandler().Handle(new DeleteRoomCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
