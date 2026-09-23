using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Commands.RemoveHotelImage;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Exceptions;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.RemoveHotelImage;

public class RemoveHotelImageCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private RemoveHotelImageCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object, _currentUser.Object);

    private Hotel GivenHotelWithImages()
    {
        var hotel = TestData.Hotel(_ownerId).WithItems("_images",
            HotelImage.Create(1, "keep.jpg", 0).WithId(7),
            HotelImage.Create(1, "drop.jpg", 1).WithId(8));
        _hotels.Setup(h => h.GetByIdWithImagesTrackedAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        return hotel;
    }

    [Fact]
    public async Task Handle_Owner_RemovesImageViaTrackedLoad_WithoutCallingUpdate()
    {
        var hotel = GivenHotelWithImages();
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        await CreateHandler().Handle(new RemoveHotelImageCommand(1, 8), CancellationToken.None);

        hotel.Images.Should().ContainSingle().Which.Url.Should().Be("keep.jpg");
        // Regression (decision #71): removing from the AsNoTracking GetByIdWithDetailsAsync graph + Update()
        // returned 204 but never deleted the row. Must use the tracked load and rely on change tracking.
        _hotels.Verify(h => h.GetByIdWithImagesTrackedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _hotels.Verify(h => h.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _hotels.Verify(h => h.Update(It.IsAny<Hotel>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownImage_ThrowsImageNotFoundAndDoesNotSave()
    {
        GivenHotelWithImages();
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var act = () => CreateHandler().Handle(new RemoveHotelImageCommand(1, 999), CancellationToken.None);

        await act.Should().ThrowAsync<ImageNotFoundException>();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbiddenAndKeepsImages()
    {
        var hotel = GivenHotelWithImages();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new RemoveHotelImageCommand(1, 8), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        hotel.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_AdminWhoDoesNotOwnTheHotel_IsAllowed()
    {
        var hotel = GivenHotelWithImages();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await CreateHandler().Handle(new RemoveHotelImageCommand(1, 8), CancellationToken.None);

        hotel.Images.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(h => h.GetByIdWithImagesTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new RemoveHotelImageCommand(1, 8), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
