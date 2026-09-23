using Application.Common.Interfaces.Persistence;
using Application.Features.HotelVisits.Common;
using Application.Features.HotelVisits.Queries.GetTrendingCities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.HotelVisits.Queries.GetTrendingCities;

public class GetTrendingCitiesQueryHandlerTests
{
    private readonly Mock<IHotelVisitRepository> _visits = new();

    [Fact]
    public async Task Handle_PassesCountAndMapsInRepositoryOrder()
    {
        _visits.Setup(v => v.GetTrendingCitiesAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new TrendingCity(3, "Paris", 40), new TrendingCity(1, "Tokyo", 25)]);

        var result = await new GetTrendingCitiesQueryHandler(_visits.Object)
            .Handle(new GetTrendingCitiesQuery(2), CancellationToken.None);

        result.Should().Equal(new TrendingCityDto(3, "Paris", 40), new TrendingCityDto(1, "Tokyo", 25));
    }
}
