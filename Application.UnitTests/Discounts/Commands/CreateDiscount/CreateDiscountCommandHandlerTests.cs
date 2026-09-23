using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Discounts.Commands.CreateDiscount;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Discounts.Commands.CreateDiscount;

public class CreateDiscountCommandHandlerTests
{
    private readonly Mock<IRoomRepository> _rooms = new();
    private readonly Mock<IDiscountRepository> _discounts = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private CreateDiscountCommandHandler CreateHandler() =>
        new(_rooms.Object, _discounts.Object, _uow.Object, _currentUser.Object);

    private static readonly CreateDiscountCommand Command =
        new(10, "Summer", DiscountType.Percentage, 20m, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));

    private void GivenRoomOwnedBy(Guid ownerId)
    {
        var room = TestData.RoomIn(TestData.Hotel(ownerId));
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(room);
    }

    [Fact]
    public async Task Handle_OwnerOfRoomsHotel_AddsDiscountAndReturnsDto()
    {
        GivenRoomOwnedBy(_ownerId);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);
        Discount? added = null;
        _discounts.Setup(d => d.AddAsync(It.IsAny<Discount>(), It.IsAny<CancellationToken>()))
            .Callback<Discount, CancellationToken>((d, _) => added = d);

        var dto = await CreateHandler().Handle(Command, CancellationToken.None);

        added.Should().NotBeNull();
        added!.RoomId.Should().Be(10);
        dto.Name.Should().Be("Summer");
        dto.Type.Should().Be("Percentage");
        dto.Value.Should().Be(20m);
        dto.StartDate.Should().Be(new DateOnly(2026, 6, 1));
        dto.EndDate.Should().Be(new DateOnly(2026, 6, 30));
        dto.IsActive.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotTheHotelsOwner_ThrowsForbiddenAndAddsNothing()
    {
        GivenRoomOwnedBy(_ownerId);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(Command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _discounts.Verify(d => d.AddAsync(It.IsAny<Discount>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoomNotFound_ThrowsNotFound()
    {
        _rooms.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Room?)null);

        var act = () => CreateHandler().Handle(Command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
