using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Commands.UpdateRoom;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Room _room;

    public UpdateRoomCommandHandlerTests()
    {
        _room = TestData.RoomIn(TestData.Hotel(_ownerId), number: "101", id: 10);
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(_room);
    }

    private UpdateRoomCommandHandler CreateHandler() => new(_rooms.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_UpdatesCapacityOnly()
    {
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var dto = await CreateHandler().Handle(new UpdateRoomCommand(10, 4, 3), CancellationToken.None);

        dto.AdultCapacity.Should().Be(4);
        dto.ChildCapacity.Should().Be(3);
        dto.Number.Should().Be("101"); // decision #22: number is immutable via Update
        dto.ModifiedAt.Should().NotBeNull();
        _rooms.Verify(r => r.Update(_room), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var dto = await CreateHandler().Handle(new UpdateRoomCommand(10, 4, 3), CancellationToken.None);

        dto.AdultCapacity.Should().Be(4);
    }

    [Fact]
    public async Task Handle_OwnerOfAnotherHotel_ThrowsForbiddenAndLeavesRoomUnchanged()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new UpdateRoomCommand(10, 4, 3), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _room.AdultCapacity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new UpdateRoomCommand(99, 4, 3), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
