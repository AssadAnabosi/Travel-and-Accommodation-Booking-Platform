using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Commands.UnblockRoomAvailability;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Exceptions;
using Domain.ValueObjects;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Commands.UnblockRoomAvailability;

public class UnblockRoomAvailabilityCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Room _room;

    public UnblockRoomAvailabilityCommandHandlerTests()
    {
        _room = TestData.RoomIn(TestData.Hotel(_ownerId), id: 10);
        _room.Block(DateRange.Of(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5))).WithId(1);
        _room.Reserve(DateRange.Of(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)), Guid.NewGuid()).WithId(2);
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(_room);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);
    }

    private UnblockRoomAvailabilityCommandHandler CreateHandler() =>
        new(_rooms.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_RemovesTheBlockAndSaves()
    {
        await CreateHandler().Handle(new UnblockRoomAvailabilityCommand(10, 1), CancellationToken.None);

        _room.Availabilities.Should().ContainSingle().Which.Id.Should().Be(2);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_BookedRange_ThrowsInvalidStateTransition()
    {
        // A booking's reservation can't be "unblocked" — it has to go through booking cancellation.
        var act = () => CreateHandler().Handle(new UnblockRoomAvailabilityCommand(10, 2), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidStateTransitionException>();
        _room.Availabilities.Should().HaveCount(2);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownAvailability_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new UnblockRoomAvailabilityCommand(10, 99), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OwnerOfAnotherHotel_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new UnblockRoomAvailabilityCommand(10, 1), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _room.Availabilities.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new UnblockRoomAvailabilityCommand(99, 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
