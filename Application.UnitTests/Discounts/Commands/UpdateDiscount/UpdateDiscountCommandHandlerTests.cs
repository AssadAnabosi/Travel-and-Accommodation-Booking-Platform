using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Discounts.Commands.UpdateDiscount;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Discounts.Commands.UpdateDiscount;

public class UpdateDiscountCommandHandlerTests
{
    private readonly Mock<IDiscountRepository> _discounts = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private UpdateDiscountCommandHandler CreateHandler() => new(_discounts.Object, _uow.Object, _currentUser.Object);

    private static readonly UpdateDiscountCommand Command =
        new(100, "Autumn", DiscountType.FixedAmount, 15m, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15));

    private Discount GivenDiscountOwnedBy(Guid ownerId)
    {
        var room = TestData.RoomIn(TestData.Hotel(ownerId));
        var discount = TestData.DiscountOn(room, DiscountType.Percentage, 10m,
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));
        _discounts.Setup(d => d.GetByIdWithRoomAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(discount);
        return discount;
    }

    [Fact]
    public async Task Handle_Owner_UpdatesDiscountAndReturnsDto()
    {
        var discount = GivenDiscountOwnedBy(_ownerId);
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var dto = await CreateHandler().Handle(Command, CancellationToken.None);

        discount.Name.Should().Be("Autumn");
        discount.Type.Should().Be(DiscountType.FixedAmount);
        discount.Value.Should().Be(15m);
        dto.Id.Should().Be(100);
        dto.Type.Should().Be("FixedAmount");
        dto.StartDate.Should().Be(new DateOnly(2026, 9, 1));
        dto.ModifiedAt.Should().NotBeNull();
        _discounts.Verify(d => d.Update(discount), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotTheHotelsOwner_ThrowsForbiddenAndLeavesDiscountUnchanged()
    {
        var discount = GivenDiscountOwnedBy(_ownerId);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(Command, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        discount.Name.Should().Be("Deal");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DiscountNotFound_ThrowsNotFound()
    {
        _discounts.Setup(d => d.GetByIdWithRoomAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Discount?)null);

        var act = () => CreateHandler().Handle(Command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
