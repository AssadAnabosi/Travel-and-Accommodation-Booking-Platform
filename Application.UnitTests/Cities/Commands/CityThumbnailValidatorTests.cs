using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Commands.CreateCity;
using Application.Features.Cities.Commands.UpdateCity;
using FluentValidation.TestHelper;
using Moq;

namespace Application.UnitTests.Cities.Commands;

/// <summary>ThumbnailUrl rules shared by CreateCity and UpdateCity: optional, absolute http(s), ≤2000 chars.</summary>
public class CityThumbnailValidatorTests
{
    private readonly Mock<ICityRepository> _cities = new(); // NameExistsAsync → false (unique name)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://img.example/paris.jpg")]
    [InlineData("http://img.example/paris.jpg")]
    public async Task ValidOrMissingThumbnail_HasNoError(string? url)
    {
        var create = await new CreateCityCommandValidator(_cities.Object)
            .TestValidateAsync(new CreateCityCommand("Paris", "France", "75001", url));
        var update = await new UpdateCityCommandValidator(_cities.Object)
            .TestValidateAsync(new UpdateCityCommand(1, "Paris", "France", "75001", url));

        create.ShouldNotHaveAnyValidationErrors();
        update.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("/relative/path.jpg")]
    [InlineData("ftp://img.example/paris.jpg")]
    [InlineData("javascript:alert(1)")]
    public async Task InvalidThumbnail_HasError(string url)
    {
        var create = await new CreateCityCommandValidator(_cities.Object)
            .TestValidateAsync(new CreateCityCommand("Paris", "France", "75001", url));
        var update = await new UpdateCityCommandValidator(_cities.Object)
            .TestValidateAsync(new UpdateCityCommand(1, "Paris", "France", "75001", url));

        create.ShouldHaveValidationErrorFor(x => x.ThumbnailUrl);
        update.ShouldHaveValidationErrorFor(x => x.ThumbnailUrl);
    }

    [Fact]
    public async Task TooLongThumbnail_HasError()
    {
        var url = "https://img.example/" + new string('a', 2000);

        var result = await new CreateCityCommandValidator(_cities.Object)
            .TestValidateAsync(new CreateCityCommand("Paris", "France", "75001", url));

        result.ShouldHaveValidationErrorFor(x => x.ThumbnailUrl);
    }
}