using Domain.Entities;
using FluentAssertions;

namespace Domain.UnitTests.Entities;

public class CityTests
{
    [Fact]
    public void Create_WithoutThumbnail_LeavesItNull()
    {
        var city = City.Create("Paris", "France", "75001");

        city.ThumbnailUrl.Should().BeNull();
    }

    [Fact]
    public void Create_WithThumbnail_TrimsAndStoresIt()
    {
        var city = City.Create("Paris", "France", "75001", "  https://img.example/paris.jpg ");

        city.ThumbnailUrl.Should().Be("https://img.example/paris.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithBlankThumbnail_ClearsIt(string? thumbnail)
    {
        var city = City.Create("Paris", "France", "75001", "https://img.example/paris.jpg");

        city.Update("Paris", "France", "75001", thumbnail);

        city.ThumbnailUrl.Should().BeNull();
        city.ModifiedAt.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithThumbnail_ReplacesIt()
    {
        var city = City.Create("Paris", "France", "75001", "https://img.example/old.jpg");

        city.Update("Paris", "France", "75001", "https://img.example/new.jpg");

        city.ThumbnailUrl.Should().Be("https://img.example/new.jpg");
    }
}