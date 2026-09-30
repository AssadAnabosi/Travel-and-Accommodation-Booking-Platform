using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Services;

namespace Infrastructure.UnitTests.Services;

public class JwtTokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "super-secret-signing-key-of-at-least-32-bytes!!",
        Issuer = "tabp",
        Audience = "tabp-clients",
        AccessTokenMinutes = 60,
        RefreshTokenDays = 7
    };

    private readonly JwtTokenService _service = new(Settings, new DateTimeProvider());

    private static User TestUser() => User.Create("ada@example.com", "hash", "Ada", "Lovelace", UserRole.HotelOwner);

    [Fact]
    public void GenerateAccessToken_EmbedsIssuerAudienceAndUserClaims()
    {
        var user = TestUser();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_service.GenerateAccessToken(user));

        jwt.Issuer.Should().Be("tabp");
        jwt.Audiences.Should().Contain("tabp-clients");
        jwt.Subject.Should().Be(user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == user.Role.ToString());
    }

    [Fact]
    public void GenerateAccessToken_SetsFutureExpiry()
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_service.GenerateAccessToken(TestUser()));

        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesUniqueNonEmptyTokens()
    {
        var first = _service.GenerateRefreshToken();
        var second = _service.GenerateRefreshToken();

        first.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
    }

    [Fact]
    public void GetRefreshTokenExpiry_IsConfiguredDaysAhead()
    {
        _service.GetRefreshTokenExpiry().Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic_AndDoesNotReturnTheRawToken()
    {
        var token = _service.GenerateRefreshToken();

        var hash = _service.HashRefreshToken(token);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(token); // the raw token is never what gets stored
        hash.Should().Be(_service.HashRefreshToken(token)); // same input -> same hash, so lookups work
    }

    [Fact]
    public void HashRefreshToken_DifferentTokens_ProduceDifferentHashes()
    {
        _service.HashRefreshToken("token-a").Should().NotBe(_service.HashRefreshToken("token-b"));
    }
}
