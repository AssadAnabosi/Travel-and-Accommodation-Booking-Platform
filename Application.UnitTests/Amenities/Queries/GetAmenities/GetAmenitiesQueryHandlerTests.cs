using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Queries.GetAmenities;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Amenities.Queries.GetAmenities;

public class GetAmenitiesQueryHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenities = new();

    [Fact]
    public async Task Handle_MapsEveryAmenityInRepositoryOrder()
    {
        _amenities.Setup(a => a.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([Amenity.Create("Pool").WithId(1), Amenity.Create("Wi-Fi").WithId(2)]);

        var result = await new GetAmenitiesQueryHandler(_amenities.Object)
            .Handle(new GetAmenitiesQuery(), CancellationToken.None);

        result.Select(a => (a.Id, a.Name)).Should().Equal((1, "Pool"), (2, "Wi-Fi"));
    }
}
