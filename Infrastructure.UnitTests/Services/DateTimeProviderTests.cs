using FluentAssertions;
using Infrastructure.Services;

namespace Infrastructure.UnitTests.Services;

public class DateTimeProviderTests
{
    private readonly DateTimeProvider _provider = new();

    [Fact]
    public void UtcNow_IsUtcAndCurrent()
    {
        _provider.UtcNow.Kind.Should().Be(DateTimeKind.Utc);
        _provider.UtcNow.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Today_IsCurrentUtcDate()
    {
        _provider.Today.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
    }
}