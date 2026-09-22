using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Users.Commands.SetUserActiveStatus;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Commands.SetUserActiveStatus;

public class SetUserActiveStatusCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private SetUserActiveStatusCommandHandler CreateHandler() => new(_users.Object, _uow.Object, _currentUser.Object);

    private static User AUser() => User.Create("u@x.com", "h", "First", "Last");

    [Fact]
    public async Task Handle_DeactivatingOwnAccount_ThrowsForbidden()
    {
        var selfId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(selfId);

        var act = () => CreateHandler().Handle(new SetUserActiveStatusCommand(selfId, IsActive: false),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeactivatingAnotherUser_Persists()
    {
        var target = Guid.NewGuid();
        var user = AUser();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _users.Setup(r => r.GetByIdAsync(target, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await CreateHandler().Handle(new SetUserActiveStatusCommand(target, IsActive: false), CancellationToken.None);

        user.IsActive.Should().BeFalse();
        _users.Verify(r => r.Update(user), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new SetUserActiveStatusCommand(Guid.NewGuid(), IsActive: true),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
