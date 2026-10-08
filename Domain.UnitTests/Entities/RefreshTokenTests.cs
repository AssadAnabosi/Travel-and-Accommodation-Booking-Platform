using Domain.Entities;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class RefreshTokenTests
{
    private static RefreshToken Active() =>
        RefreshToken.Create(Guid.NewGuid(), "tok", DateTime.UtcNow.AddHours(1));

    [Fact]
    public void Create_WithFutureExpiry_IsActive()
    {
        var token = Active();

        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithPastExpiry_IsExpiredAndInactive()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "tok", DateTime.UtcNow.AddMinutes(-1));

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_MarksRevokedInactive()
    {
        var token = Active();

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_Twice_Throws()
    {
        var token = Active();
        token.Revoke();

        var act = () => token.Revoke();

        act.Should().Throw<InvalidOperationException>();
    }
}