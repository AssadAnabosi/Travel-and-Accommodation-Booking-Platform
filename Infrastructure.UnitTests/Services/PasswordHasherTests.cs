using FluentAssertions;
using Infrastructure.Services;

namespace Infrastructure.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesNonEmptyHash_DifferentFromInput()
    {
        var hash = _hasher.Hash("Password1");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("Password1");
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Password1");

        _hasher.Verify("Password1", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Password1");

        _hasher.Verify("Password2", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // BCrypt salts each hash, so the outputs differ even for identical input.
        _hasher.Hash("Password1").Should().NotBe(_hasher.Hash("Password1"));
    }
}