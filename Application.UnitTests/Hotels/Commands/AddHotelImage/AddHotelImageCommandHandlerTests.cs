using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Commands.AddHotelImage;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Commands.AddHotelImage;

public class AddHotelImageCommandHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    private AddHotelImageCommandHandler CreateHandler() => new(_hotels.Object, _uow.Object, _currentUser.Object);

    private Hotel GivenHotelWithOneImage()
    {
        var hotel = TestData.Hotel(_ownerId).WithItems("_images", HotelImage.Create(1, "existing.jpg", 0));
        _hotels.Setup(h => h.GetByIdWithImagesTrackedAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        return hotel;
    }

    [Fact]
    public async Task Handle_Owner_AppendsImageViaTrackedLoad_WithoutCallingUpdate()
    {
        var hotel = GivenHotelWithOneImage();
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        await CreateHandler().Handle(new AddHotelImageCommand(1, "new.jpg"), CancellationToken.None);

        hotel.Images.Should().HaveCount(2);
        hotel.Images.Single(i => i.Url == "new.jpg").DisplayOrder.Should().Be(1);
        // Tracked load + change tracking, not Update() of an AsNoTracking graph.
        _hotels.Verify(h => h.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _hotels.Verify(h => h.Update(It.IsAny<Hotel>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdminWhoDoesNotOwnTheHotel_IsAllowed()
    {
        var hotel = GivenHotelWithOneImage();
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        await CreateHandler().Handle(new AddHotelImageCommand(1, "new.jpg"), CancellationToken.None);

        hotel.Images.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbiddenAndAddsNothing()
    {
        var hotel = GivenHotelWithOneImage();
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new AddHotelImageCommand(1, "new.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        hotel.Images.Should().ContainSingle();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        _hotels.Setup(h => h.GetByIdWithImagesTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new AddHotelImageCommand(1, "new.jpg"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
