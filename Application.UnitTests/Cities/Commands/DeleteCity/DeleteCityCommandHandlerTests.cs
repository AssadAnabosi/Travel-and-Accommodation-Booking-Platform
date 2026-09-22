using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Commands.DeleteCity;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Cities.Commands.DeleteCity;

public class DeleteCityCommandHandlerTests
{
    private readonly Mock<ICityRepository> _cities = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DeleteCityCommandHandler CreateHandler() => new(_cities.Object, _uow.Object);

    private City GivenCity()
    {
        var city = City.Create("Paris", "France", "75001");
        _cities.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(city);
        return city;
    }

    [Fact]
    public async Task Handle_NoHotels_Deletes()
    {
        var city = GivenCity();
        _cities.Setup(r => r.HasHotelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler().Handle(new DeleteCityCommand(1), CancellationToken.None);

        _cities.Verify(r => r.Remove(city), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CityHasHotels_ThrowsConflict_AndDoesNotDelete()
    {
        GivenCity();
        _cities.Setup(r => r.HasHotelsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new DeleteCityCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _cities.Verify(r => r.Remove(It.IsAny<City>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CityNotFound_ThrowsNotFound()
    {
        _cities.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((City?)null);

        var act = () => CreateHandler().Handle(new DeleteCityCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
