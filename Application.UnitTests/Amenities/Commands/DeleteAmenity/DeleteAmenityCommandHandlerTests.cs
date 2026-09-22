using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Amenities.Commands.DeleteAmenity;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Amenities.Commands.DeleteAmenity;

public class DeleteAmenityCommandHandlerTests
{
    private readonly Mock<IAmenityRepository> _amenities = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DeleteAmenityCommandHandler CreateHandler() => new(_amenities.Object, _uow.Object);

    private Amenity GivenAmenity()
    {
        var amenity = Amenity.Create("Free WiFi");
        _amenities.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(amenity);
        return amenity;
    }

    [Fact]
    public async Task Handle_NotInUse_Deletes()
    {
        var amenity = GivenAmenity();
        _amenities.Setup(r => r.IsInUseAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await CreateHandler().Handle(new DeleteAmenityCommand(1), CancellationToken.None);

        _amenities.Verify(r => r.Remove(amenity), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AmenityInUse_ThrowsConflict_AndDoesNotDelete()
    {
        GivenAmenity();
        _amenities.Setup(r => r.IsInUseAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => CreateHandler().Handle(new DeleteAmenityCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _amenities.Verify(r => r.Remove(It.IsAny<Amenity>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AmenityNotFound_ThrowsNotFound()
    {
        _amenities.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Amenity?)null);

        var act = () => CreateHandler().Handle(new DeleteAmenityCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
