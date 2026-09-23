using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Hotels.Commands.ReassignHotelOwner;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.ReassignHotelOwner;

public class ReassignHotelOwnerCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ReassignHotelOwnerCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object);

    [Fact]
    public async Task Handle_DifferentOwner_ReassignsAndSaves()
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid());
        var newOwnerId = Guid.NewGuid();
        _hotels.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        await CreateHandler().Handle(new ReassignHotelOwnerCommand(1, newOwnerId), CancellationToken.None);

        hotel.OwnerId.Should().Be(newOwnerId);
        _hotels.Verify(r => r.Update(hotel), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SameOwner_IsANoOp()
    {
        var ownerId = Guid.NewGuid();
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, ownerId);
        _hotels.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        await CreateHandler().Handle(new ReassignHotelOwnerCommand(1, ownerId), CancellationToken.None);

        _hotels.Verify(r => r.Update(It.IsAny<Hotel>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new ReassignHotelOwnerCommand(1, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
