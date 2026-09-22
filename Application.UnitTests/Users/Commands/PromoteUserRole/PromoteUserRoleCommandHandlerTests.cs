using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Users.Commands.PromoteUserRole;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Commands.PromoteUserRole;

public class PromoteUserRoleCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private PromoteUserRoleCommandHandler CreateHandler() => new(_users.Object, _uow.Object, _currentUser.Object);

    private static User UserWithRole(UserRole role) => User.Create("u@x.com", "h", "First", "Last", role);

    [Fact]
    public async Task Handle_PromoteAnotherUser_Persists()
    {
        var target = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid()); // an admin acting on someone else
        _users.Setup(r => r.GetByIdAsync(target, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserWithRole(UserRole.Customer));

        await CreateHandler().Handle(new PromoteUserRoleCommand(target, UserRole.HotelOwner), CancellationToken.None);

        _users.Verify(r => r.Update(It.IsAny<User>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminRemovingOwnAdminRole_ThrowsForbidden()
    {
        var selfId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(selfId);
        _users.Setup(r => r.GetByIdAsync(selfId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserWithRole(UserRole.Admin));

        var act = () => CreateHandler().Handle(new PromoteUserRoleCommand(selfId, UserRole.Customer),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new PromoteUserRoleCommand(Guid.NewGuid(), UserRole.HotelOwner),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
