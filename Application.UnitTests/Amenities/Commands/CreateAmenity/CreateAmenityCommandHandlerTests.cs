using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Commands.CreateAmenity;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Amenities.Commands.CreateAmenity;

public class CreateAmenityCommandHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenities = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task Handle_AddsAmenityAndReturnsDto()
    {
        Amenity? added = null;
        _amenities.Setup(a => a.AddAsync(It.IsAny<Amenity>(), It.IsAny<CancellationToken>()))
            .Callback<Amenity, CancellationToken>((a, _) => added = a);

        var dto = await new CreateAmenityCommandHandler(_amenities.Object, _uow.Object)
            .Handle(new CreateAmenityCommand("Rooftop Bar"), CancellationToken.None);

        added!.Name.Should().Be("Rooftop Bar");
        dto.Name.Should().Be("Rooftop Bar");
        dto.ModifiedAt.Should().BeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
