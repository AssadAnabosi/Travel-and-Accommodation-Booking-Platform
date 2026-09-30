using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Auth.Commands.Register;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Auth.Commands.Register;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private RegisterCommandHandler CreateHandler() =>
        new(_users.Object, _uow.Object, _hasher.Object, _jwt.Object);

    private static RegisterCommand Command() => new("Ada@Example.com", "Password1", "Ada", "Lovelace");

    [Fact]
    public async Task Handle_NewEmail_CreatesUserWithNormalizedEmailAndPersists()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _jwt.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns("refresh-token");
        _jwt.Setup(j => j.HashRefreshToken("refresh-token")).Returns("refresh-token-hash");
        _jwt.Setup(j => j.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        result.Email.Should().Be("ada@example.com"); // normalized to lower-case
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token"); // client receives the raw token, not the hash
        _users.Verify(r => r.AddAsync(It.Is<User>(u => u.PasswordHash == "hashed"), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ThrowsValidationException_AndDoesNotPersist()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
