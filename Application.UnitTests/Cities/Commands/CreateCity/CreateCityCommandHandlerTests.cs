using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Commands.CreateCity;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Cities.Commands.CreateCity;

public class CreateCityCommandHandlerTests
{
    private readonly Mock<ICityRepository> _cities = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task Handle_AddsCityAndReturnsDtoWithZeroHotels()
    {
        City? added = null;
        _cities.Setup(c => c.AddAsync(It.IsAny<City>(), It.IsAny<CancellationToken>()))
            .Callback<City, CancellationToken>((c, _) => added = c);

        var dto = await new CreateCityCommandHandler(_cities.Object, _uow.Object)
            .Handle(new CreateCityCommand("Lyon", "France", "69000"), CancellationToken.None);

        added!.Name.Should().Be("Lyon");
        dto.Name.Should().Be("Lyon");
        dto.Country.Should().Be("France");
        dto.PostOffice.Should().Be("69000");
        dto.HotelsCount.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
