using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Auth.Commands.Login;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Auth.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private LoginCommandHandler CreateHandler() =>
        new(_users.Object, _uow.Object, _hasher.Object, _jwt.Object);

    private static User ActiveUser() => User.Create("ada@example.com", "stored-hash", "Ada", "Lovelace");

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokensAndPersists()
    {
        var user = ActiveUser();
        var expiry = DateTime.UtcNow.AddDays(7);
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns("access-token");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwt.Setup(j => j.GetRefreshTokenExpiry()).Returns(expiry);

        var result = await CreateHandler().Handle(new LoginCommand("ada@example.com", "pw"), CancellationToken.None);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        _users.Verify(r => r.Update(user), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsUnauthorized()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => CreateHandler().Handle(new LoginCommand("x@y.com", "pw"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveUser());
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var act = () => CreateHandler().Handle(new LoginCommand("ada@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_DeactivatedAccount_ThrowsUnauthorized()
    {
        var user = ActiveUser();
        user.Deactivate();
        _users.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var act = () => CreateHandler().Handle(new LoginCommand("ada@example.com", "pw"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }
}
