using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Commands.UpdateAmenity;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Amenities.Commands.UpdateAmenity;

public class UpdateAmenityCommandHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenities = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateAmenityCommandHandler CreateHandler() => new(_amenities.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingAmenity_RenamesAndSaves()
    {
        var amenity = Amenity.Create("Wifi").WithId(3);
        _amenities.Setup(a => a.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(amenity);

        var dto = await CreateHandler().Handle(new UpdateAmenityCommand(3, "Wi-Fi"), CancellationToken.None);

        amenity.Name.Should().Be("Wi-Fi");
        dto.Id.Should().Be(3);
        dto.Name.Should().Be("Wi-Fi");
        _amenities.Verify(a => a.Update(amenity), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new UpdateAmenityCommand(3, "Wi-Fi"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
