using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Queries.GetRoomById;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Queries.GetRoomById;

public class GetRoomByIdQueryHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public GetRoomByIdQueryHandlerTests()
    {
        var room = TestData.RoomIn(TestData.Hotel(_ownerId, id: 3), basePrice: 150m, number: "305", id: 10,
            type: RoomType.Deluxe);
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(room);
    }

    private GetRoomByIdQueryHandler CreateHandler() => new(_rooms.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_ReturnsMappedRoom()
    {
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var dto = await CreateHandler().Handle(new GetRoomByIdQuery(10), CancellationToken.None);

        dto.Id.Should().Be(10);
        dto.HotelId.Should().Be(3);
        dto.Number.Should().Be("305");
        dto.RoomType.Should().Be("Deluxe");
        dto.AdultCapacity.Should().Be(2);
        dto.ChildCapacity.Should().Be(1);
        dto.BasePrice.Should().Be(150m);
        dto.Currency.Should().Be("USD");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var dto = await CreateHandler().Handle(new GetRoomByIdQuery(10), CancellationToken.None);

        dto.Id.Should().Be(10);
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetRoomByIdQuery(10), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new GetRoomByIdQuery(99), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
