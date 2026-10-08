using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Auth.Commands.RefreshToken;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private RefreshTokenCommandHandler CreateHandler() => new(_users.Object, _uow.Object, _jwt.Object);

    // Stores the token's hash, mirroring production where the raw value never touches the DB.
    private static User UserWithToken(string tokenHash)
    {
        var user = User.Create("ada@example.com", "hash", "Ada", "Lovelace");
        user.IssueRefreshToken(tokenHash, DateTime.UtcNow.AddDays(7));
        return user;
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesAndInsertsNewToken()
    {
        var user = UserWithToken("old-token-hash");
        _jwt.Setup(j => j.HashRefreshToken("old-token")).Returns("old-token-hash");
        _users.Setup(r => r.GetByRefreshTokenAsync("old-token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("new-token");
        _jwt.Setup(j => j.HashRefreshToken("new-token")).Returns("new-token-hash");
        _jwt.Setup(j => j.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");

        var result = await CreateHandler().Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("new-token"); // client receives the raw token, not the hash
        // Old token revoked and chained to the replacement (rotation).
        user.FindActiveRefreshToken("old-token-hash").Should().BeNull();
        // Regression: the new token must be inserted via AddRefreshToken, not Update(user); only the hash is stored.
        _users.Verify(r => r.AddRefreshToken(It.Is<Domain.Entities.RefreshToken>(t => t.TokenHash == "new-token-hash")),
            Times.Once);
        _users.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsUnauthorized()
    {
        _users.Setup(r => r.GetByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("nope"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsUnauthorized()
    {
        var user = UserWithToken("old-token-hash");
        user.FindActiveRefreshToken("old-token-hash")!.Revoke();
        _jwt.Setup(j => j.HashRefreshToken("old-token")).Returns("old-token-hash");
        _users.Setup(r => r.GetByRefreshTokenAsync("old-token-hash", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var act = () => CreateHandler().Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
