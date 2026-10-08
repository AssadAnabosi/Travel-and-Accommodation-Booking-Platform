using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Commands.CreateHotel;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.CreateHotel;

public class CreateHotelCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<ICityRepository> _cities = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    public CreateHotelCommandHandlerTests()
    {
        // Needed only for the returned DTO projection.
        _cities.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(City.Create("Paris", "France", "75001"));
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create("o@x.com", "h", "Olivia", "Owner"));
    }

    private CreateHotelCommandHandler CreateHandler() =>
        new(_hotels.Object, _cities.Object, _users.Object, _uow.Object, _currentUser.Object);

    private static CreateHotelCommand Command(Guid? ownerId) =>
        new("Grand", 5, "d", "addr", 1.0, 2.0, 1, ownerId);

    [Fact]
    public async Task Handle_Owner_CreatesPendingHotelOwnedByCaller()
    {
        var callerId = Guid.NewGuid();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(callerId);

        var result = await CreateHandler().Handle(Command(ownerId: null), CancellationToken.None);

        result.ApprovalStatus.Should().Be("Pending");
        result.OwnerId.Should().Be(callerId);
        _hotels.Verify(r => r.AddAsync(It.IsAny<Hotel>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_CreatesApprovedHotelForAssignedOwner()
    {
        var ownerId = Guid.NewGuid();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var result = await CreateHandler().Handle(Command(ownerId), CancellationToken.None);

        result.ApprovalStatus.Should().Be("Approved");
        result.OwnerId.Should().Be(ownerId);
        _hotels.Verify(r => r.AddAsync(It.IsAny<Hotel>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
