using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Commands.CreateRoom;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Commands.CreateRoom;

public class CreateRoomCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public CreateRoomCommandHandlerTests() =>
        _hotels.Setup(h => h.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Hotel(_ownerId, approved: false, id: 3));

    private CreateRoomCommandHandler CreateHandler() =>
        new(_hotels.Object, _rooms.Object, _uow.Object, _currentUser.Object);

    private static readonly CreateRoomCommand Command = new(3, "201", RoomType.Suite, 3, 2, 250m, "eur");

    [Fact]
    public async Task Handle_Owner_AddsRoomEvenToAPendingHotel()
    {
        // Rooms can be added regardless of the hotel's approval status.
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);
        Room? added = null;
        _rooms.Setup(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()))
            .Callback<Room, CancellationToken>((r, _) => added = r);

        var dto = await CreateHandler().Handle(Command, CancellationToken.None);

        added!.HotelId.Should().Be(3);
        dto.HotelId.Should().Be(3);
        dto.Number.Should().Be("201");
        dto.RoomType.Should().Be("Suite");
        dto.AdultCapacity.Should().Be(3);
        dto.ChildCapacity.Should().Be(2);
        dto.BasePrice.Should().Be(250m);
        dto.Currency.Should().Be("EUR"); // Money normalizes the currency code
        dto.IsActive.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await CreateHandler().Handle(Command, CancellationToken.None);

        _rooms.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OwnerOfAnotherHotel_ThrowsForbiddenAndAddsNothing()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(Command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _rooms.Verify(r => r.AddAsync(It.IsAny<Room>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(Command with { HotelId = 99 }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
