using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Commands.UpdateHotelAmenities;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.UpdateHotelAmenities;

public class UpdateHotelAmenitiesCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private UpdateHotelAmenitiesCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object, _currentUser.Object);

    private static Hotel AHotel(Guid ownerId) =>
        Hotel.CreateByOwner("Grand", 5, "d", "addr", 1.0, 2.0, cityId: 1, ownerId);

    [Fact]
    public async Task Handle_Admin_SetsAmenitiesViaTrackedLoad_WithoutCallingUpdate()
    {
        var hotel = AHotel(Guid.NewGuid());
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _hotels.Setup(r => r.GetByIdWithAmenitiesTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        await CreateHandler().Handle(new UpdateHotelAmenitiesCommand(1, new[] { 1, 2, 3 }), CancellationToken.None);

        hotel.HotelAmenities.Should().HaveCount(3);
        // Regression: must use the tracked load and rely on change tracking, not Update().
        _hotels.Verify(r => r.GetByIdWithAmenitiesTrackedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _hotels.Verify(r => r.Update(It.IsAny<Hotel>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotOwnerNotAdmin_ThrowsForbidden()
    {
        var hotel = AHotel(Guid.NewGuid()); // owned by someone else
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
        _hotels.Setup(r => r.GetByIdWithAmenitiesTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hotel);

        var act = () => CreateHandler().Handle(new UpdateHotelAmenitiesCommand(1, new[] { 1 }), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _hotels.Setup(r => r.GetByIdWithAmenitiesTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new UpdateHotelAmenitiesCommand(1, new[] { 1 }), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
