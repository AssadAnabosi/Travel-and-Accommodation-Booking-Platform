using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Hotels.Commands.ApproveHotel;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.ApproveHotel;

public class ApproveHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ApproveHotelCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object);

    [Fact]
    public async Task Handle_PendingHotel_Approves()
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid()); // Pending
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        await CreateHandler().Handle(new ApproveHotelCommand(1), CancellationToken.None);

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Approved);
        _hotels.Verify(r => r.Update(hotel), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new ApproveHotelCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
