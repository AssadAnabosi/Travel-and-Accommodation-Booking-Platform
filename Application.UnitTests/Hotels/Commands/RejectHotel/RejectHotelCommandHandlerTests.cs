using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Hotels.Commands.RejectHotel;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.RejectHotel;

public class RejectHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private RejectHotelCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object);

    [Fact]
    public async Task Handle_PendingHotel_RejectsWithReason()
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid()); // Pending
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        await CreateHandler().Handle(new RejectHotelCommand(1, "bad photos"), CancellationToken.None);

        hotel.ApprovalStatus.Should().Be(HotelApprovalStatus.Rejected);
        hotel.RejectionReason.Should().Be("bad photos");
        _hotels.Verify(r => r.Update(hotel), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new RejectHotelCommand(1, "x"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
