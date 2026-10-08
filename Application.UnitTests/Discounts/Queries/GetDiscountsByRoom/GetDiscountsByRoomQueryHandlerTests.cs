using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Discounts.Common;
using Application.Features.Discounts.Queries.GetDiscountsByRoom;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Discounts.Queries.GetDiscountsByRoom;

public class GetDiscountsByRoomQueryHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IDiscountRepository> _discounts = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private GetDiscountsByRoomQueryHandler CreateHandler() =>
        new(_rooms.Object, _discounts.Object, _currentUser.Object);

    private void GivenRoomWithDiscounts()
    {
        var room = TestData.RoomIn(TestData.Hotel(_ownerId));
        var a = TestData.DiscountOn(room, DiscountType.Percentage, 10m, new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30), id: 1);
        var b = TestData.DiscountOn(room, DiscountType.FixedAmount, 25m, new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31), id: 2);
        b.Deactivate();
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(room);
        _discounts.Setup(d => d.GetByRoomIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync([a, b]);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsAllDiscountsIncludingInactive()
    {
        GivenRoomWithDiscounts();
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var result = await CreateHandler().Handle(new GetDiscountsByRoomQuery(10), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Should().Match<DiscountDto>(d =>
            d.Id == 1 && d.RoomId == 10 && d.Type == "Percentage" && d.Value == 10m && d.IsActive);
        result[1].Should().Match<DiscountDto>(d =>
            d.Id == 2 && d.Type == "FixedAmount" && !d.IsActive);
    }

    [Fact]
    public async Task Handle_AdminWhoDoesNotOwnTheHotel_IsAllowed()
    {
        GivenRoomWithDiscounts();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(new GetDiscountsByRoomQuery(10), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbiddenWithoutQueryingDiscounts()
    {
        GivenRoomWithDiscounts();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetDiscountsByRoomQuery(10), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _discounts.Verify(d => d.GetByRoomIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var act = () => CreateHandler().Handle(new GetDiscountsByRoomQuery(10), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
