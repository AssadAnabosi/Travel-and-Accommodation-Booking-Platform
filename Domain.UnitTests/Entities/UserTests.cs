using Domain.Entities;
using Domain.Enums;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class UserTests
{
    private static User NewUser() => User.Create("a@b.com", "hash", "Ada", "Lovelace");

    [Fact]
    public void Create_DefaultsToCustomer_AndActive()
    {
        var user = NewUser();

        user.Role.Should().Be(UserRole.Customer);
        user.IsActive.Should().BeTrue();
        user.Email.Should().Be("a@b.com");
    }

    [Fact]
    public void Create_WithExplicitRole_UsesIt()
    {
        User.Create("a@b.com", "hash", "Ada", "Lovelace", UserRole.Admin).Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void UpdateProfile_ChangesNames()
    {
        var user = NewUser();

        user.UpdateProfile("Grace", "Hopper");

        user.FirstName.Should().Be("Grace");
        user.LastName.Should().Be("Hopper");
    }

    [Fact]
    public void ChangePassword_UpdatesHash()
    {
        var user = NewUser();

        user.ChangePassword("new-hash");

        user.PasswordHash.Should().Be("new-hash");
    }

    [Fact]
    public void ChangePassword_Blank_Throws()
    {
        var act = () => NewUser().ChangePassword("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DeactivateThenActivate_TogglesIsActive()
    {
        var user = NewUser();

        user.Deactivate();
        user.IsActive.Should().BeFalse();

        user.Activate();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void PromoteToRole_ChangesRole()
    {
        var user = NewUser();

        user.PromoteToRole(UserRole.HotelOwner);

        user.Role.Should().Be(UserRole.HotelOwner);
    }

    [Fact]
    public void IssueRefreshToken_IsFindableWhileActive()
    {
        var user = NewUser();

        var token = user.IssueRefreshToken("tok-1", DateTime.UtcNow.AddHours(1));

        user.RefreshTokens.Should().Contain(token);
        user.FindActiveRefreshToken("tok-1").Should().Be(token);
    }

    [Fact]
    public void FindActiveRefreshToken_AfterRevoke_ReturnsNull()
    {
        var user = NewUser();
        var token = user.IssueRefreshToken("tok-1", DateTime.UtcNow.AddHours(1));

        token.Revoke();

        user.FindActiveRefreshToken("tok-1").Should().BeNull();
    }

    [Fact]
    public void FindActiveRefreshToken_UnknownToken_ReturnsNull()
    {
        NewUser().FindActiveRefreshToken("nope").Should().BeNull();
    }
}