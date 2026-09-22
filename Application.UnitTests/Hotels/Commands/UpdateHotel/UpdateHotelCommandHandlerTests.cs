using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Commands.UpdateHotel;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.UpdateHotel;

public class UpdateHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<ICityRepository> _cities = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public UpdateHotelCommandHandlerTests()
    {
        _cities.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(City.Create("Paris", "France", "75001"));
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create("o@x.com", "h", "Olivia", "Owner"));
    }

    private UpdateHotelCommandHandler CreateHandler() =>
        new(_hotels.Object, _cities.Object, _users.Object, _uow.Object, _currentUser.Object);

    private static UpdateHotelCommand Command() => new(1, "New Name", 4, "d2", "addr2", 3.0, 4.0, 1);

    private static Hotel RejectedHotelOwnedBy(Guid ownerId)
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, ownerId);
        hotel.Reject("bad photos");
        return hotel;
    }

    [Fact]
    public async Task Handle_OwnerEditsRejectedHotel_AutoResubmitsToPending()
    {
        var ownerId = Guid.NewGuid();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RejectedHotelOwnedBy(ownerId));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.ApprovalStatus.Should().Be("Pending");
        _hotels.Verify(r => r.Update(It.IsAny<Hotel>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminEditsRejectedHotel_StaysRejected()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RejectedHotelOwnedBy(Guid.NewGuid()));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.ApprovalStatus.Should().Be("Rejected");
    }

    [Fact]
    public async Task Handle_NotOwnerNotAdmin_ThrowsForbidden()
    {
        var hotel = Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, 1, Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(hotel);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _hotels.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
