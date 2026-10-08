using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Rooms.Queries.GetRoomsByHotel;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Rooms.Queries.GetRoomsByHotel;

public class GetRoomsByHotelQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public GetRoomsByHotelQueryHandlerTests()
    {
        var hotel = TestData.Hotel(_ownerId, id: 3);
        var active = TestData.RoomIn(hotel, number: "101", id: 1);
        var retired = TestData.RoomIn(hotel, number: "102", id: 2);
        retired.Retire();
        _hotels.Setup(h => h.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        _rooms.Setup(r => r.GetByHotelIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync([active, retired]);
    }

    private GetRoomsByHotelQueryHandler CreateHandler() => new(_hotels.Object, _rooms.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_ReturnsAllRoomsIncludingInactive()
    {
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var result = await CreateHandler().Handle(new GetRoomsByHotelQuery(3), CancellationToken.None);

        result.Select(r => (r.Number, r.IsActive)).Should().Equal(("101", true), ("102", false));
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);

        var result = await CreateHandler().Handle(new GetRoomsByHotelQuery(3), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbiddenWithoutLoadingRooms()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetRoomsByHotelQuery(3), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _rooms.Verify(r => r.GetByHotelIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(h => h.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new GetRoomsByHotelQuery(3), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
