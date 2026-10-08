using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Cities.Queries.GetCities;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Cities.Queries.GetCities;

public class GetCitiesQueryHandlerTests
{
    private readonly Mock<ICityRepository> _cities = new();

    [Fact]
    public async Task Handle_PassesKeywordAndPagingAndCountsHotels()
    {
        var paris = City.Create("Paris", "France", "75000").WithId(3)
            .WithItems("_hotels", TestData.Hotel(Guid.NewGuid(), id: 1), TestData.Hotel(Guid.NewGuid(), id: 2));
        _cities.Setup(c => c.SearchAsync("par", 1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<City>([paris], totalCount: 1, pageNumber: 1, pageSize: 5));

        var page = await new GetCitiesQueryHandler(_cities.Object)
            .Handle(new GetCitiesQuery("par", PageNumber: 1, PageSize: 5), CancellationToken.None);

        page.TotalCount.Should().Be(1);
        var city = page.Items.Should().ContainSingle().Subject;
        city.Id.Should().Be(3);
        city.Name.Should().Be("Paris");
        city.Country.Should().Be("France");
        city.PostOffice.Should().Be("75000");
        // Regression: HotelsCount comes from the eager-loaded Hotels collection.
        city.HotelsCount.Should().Be(2);
    }
}
