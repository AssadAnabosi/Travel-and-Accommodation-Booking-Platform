using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Users.Commands.ChangePassword;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Commands.ChangePassword;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly User _user = User.Create("ada@tabp.dev", "old-hash", "Ada", "Lovelace");

    public ChangePasswordCommandHandlerTests()
    {
        _currentUser.Setup(c => c.UserId).Returns(_user.Id);
        _users.Setup(u => u.GetByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
        _hasher.Setup(h => h.Hash("N3w-Passw0rd!")).Returns("new-hash");
    }

    private ChangePasswordCommandHandler CreateHandler() =>
        new(_users.Object, _uow.Object, _currentUser.Object, _hasher.Object);

    [Fact]
    public async Task Handle_CorrectCurrentPassword_StoresNewHash()
    {
        _hasher.Setup(h => h.Verify("0ld-Passw0rd!", "old-hash")).Returns(true);

        await CreateHandler().Handle(new ChangePasswordCommand("0ld-Passw0rd!", "N3w-Passw0rd!"),
            CancellationToken.None);

        _user.PasswordHash.Should().Be("new-hash");
        _users.Verify(u => u.Update(_user), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ThrowsUnauthorizedAndKeepsOldHash()
    {
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "old-hash")).Returns(false);

        var act = () => CreateHandler().Handle(new ChangePasswordCommand("guess", "N3w-Passw0rd!"),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _user.PasswordHash.Should().Be("old-hash");
        _hasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNoLongerExists_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new ChangePasswordCommand("a", "b"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
