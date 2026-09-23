using Application.Common.Interfaces.Persistence;
using Application.Features.Auth.Commands.Logout;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private LogoutCommandHandler CreateHandler() => new(_users.Object, _uow.Object);

    [Fact]
    public async Task Handle_ActiveToken_RevokesItAndSaves()
    {
        var user = User.Create("ada@tabp.dev", "hash", "Ada", "Lovelace");
        var token = user.IssueRefreshToken("tok-1", DateTime.UtcNow.AddDays(7));
        var other = user.IssueRefreshToken("tok-2", DateTime.UtcNow.AddDays(7));
        _users.Setup(u => u.GetByRefreshTokenAsync("tok-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await CreateHandler().Handle(new LogoutCommand("tok-1"), CancellationToken.None);

        token.IsRevoked.Should().BeTrue();
        other.IsRevoked.Should().BeFalse(); // only this session is logged out
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownToken_IsANoOp()
    {
        _users.Setup(u => u.GetByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await CreateHandler().Handle(new LogoutCommand("nope"), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_IsANoOp()
    {
        // Logout is idempotent: a second call (or a stale cookie) must not throw or re-save.
        var user = User.Create("ada@tabp.dev", "hash", "Ada", "Lovelace");
        user.IssueRefreshToken("tok-1", DateTime.UtcNow.AddDays(7)).Revoke();
        _users.Setup(u => u.GetByRefreshTokenAsync("tok-1", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        await CreateHandler().Handle(new LogoutCommand("tok-1"), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
