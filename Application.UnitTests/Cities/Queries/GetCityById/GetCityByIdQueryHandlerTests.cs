using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Queries.GetCityById;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Cities.Queries.GetCityById;

public class GetCityByIdQueryHandlerTests
{
    private readonly Mock<ICityRepository> _cities = new();

    [Fact]
    public async Task Handle_ExistingCity_MapsIncludingHotelsCount()
    {
        var tokyo = City.Create("Tokyo", "Japan", "100-0001").WithId(4)
            .WithItems("_hotels", TestData.Hotel(Guid.NewGuid()));
        _cities.Setup(c => c.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(tokyo);

        var dto = await new GetCityByIdQueryHandler(_cities.Object)
            .Handle(new GetCityByIdQuery(4), CancellationToken.None);

        dto.Id.Should().Be(4);
        dto.Name.Should().Be("Tokyo");
        dto.HotelsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CityNotFound_ThrowsNotFound()
    {
        _cities.Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((City?)null);

        var act = () => new GetCityByIdQueryHandler(_cities.Object).Handle(new GetCityByIdQuery(4),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
