using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Commands.BlockRoomAvailability;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Commands.BlockRoomAvailability;

public class BlockRoomAvailabilityCommandHandlerTests
{
    private static readonly DateOnly Start = new(2026, 9, 1);
    private static readonly DateOnly End = new(2026, 9, 5);

    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Room _room;

    public BlockRoomAvailabilityCommandHandlerTests()
    {
        _room = TestData.RoomIn(TestData.Hotel(_ownerId), id: 10);
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(_room);
    }

    private BlockRoomAvailabilityCommandHandler CreateHandler() =>
        new(_rooms.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_AddsBlockedRangeAndSaves()
    {
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        await CreateHandler().Handle(new BlockRoomAvailabilityCommand(10, Start, End), CancellationToken.None);

        var block = _room.Availabilities.Should().ContainSingle().Subject;
        block.Status.Should().Be(AvailabilityStatus.Blocked);
        block.Range.Should().Be(DateRange.Of(Start, End));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OverlapsExistingBooking_ThrowsRoomNotAvailableAndDoesNotSave()
    {
        // Mapped to 409 by the global handler (decision #68).
        _room.Reserve(DateRange.Of(Start.AddDays(2), End.AddDays(2)), Guid.NewGuid());
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var act = () => CreateHandler().Handle(new BlockRoomAvailabilityCommand(10, Start, End),
            CancellationToken.None);

        await act.Should().ThrowAsync<RoomNotAvailableException>();
        _room.Availabilities.Should().ContainSingle();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EndBeforeStart_ThrowsInvalidDateRange()
    {
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var act = () => CreateHandler().Handle(new BlockRoomAvailabilityCommand(10, End, Start),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDateRangeException>();
    }

    [Fact]
    public async Task Handle_OwnerOfAnotherHotel_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new BlockRoomAvailabilityCommand(10, Start, End),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _room.Availabilities.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new BlockRoomAvailabilityCommand(99, Start, End),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
