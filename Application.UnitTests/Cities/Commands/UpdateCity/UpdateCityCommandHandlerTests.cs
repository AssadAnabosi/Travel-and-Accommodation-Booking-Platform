using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Cities.Commands.UpdateCity;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Cities.Commands.UpdateCity;

public class UpdateCityCommandHandlerTests
{
    private readonly Mock<ICityRepository> _cities = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateCityCommandHandler CreateHandler() => new(_cities.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingCity_UpdatesAndKeepsHotelsCount()
    {
        var city = City.Create("Pariss", "France", "75000").WithId(2)
            .WithItems("_hotels", TestData.Hotel(Guid.NewGuid()));
        _cities.Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        var dto = await CreateHandler().Handle(new UpdateCityCommand(2, "Paris", "France", "75001"),
            CancellationToken.None);

        city.Name.Should().Be("Paris");
        dto.Name.Should().Be("Paris");
        dto.PostOffice.Should().Be("75001");
        dto.HotelsCount.Should().Be(1);
        _cities.Verify(c => c.Update(city), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CityNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new UpdateCityCommand(2, "Paris", "France", "75001"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
