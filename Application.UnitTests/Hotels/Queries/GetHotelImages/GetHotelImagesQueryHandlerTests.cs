using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Queries.GetHotelImages;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetHotelImages;

public class GetHotelImagesQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private GetHotelImagesQueryHandler CreateHandler() => new(_hotels.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_OwnerOfPendingHotel_ReturnsIdsInDisplayOrder()
    {
        var ownerId = Guid.NewGuid();
        var hotel = TestData.Hotel(ownerId, approved: false);
        hotel.AddImage("https://img.example/a.jpg").WithId(21);
        hotel.AddImage("https://img.example/b.jpg").WithId(20);
        _hotels.Setup(r => r.GetByIdWithImagesTrackedAsync(hotel.Id, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(ownerId);

        var images = await CreateHandler().Handle(new GetHotelImagesQuery(hotel.Id), CancellationToken.None);

        images.Select(i => (i.Id, i.Url, i.DisplayOrder))
            .Should().Equal((21, "https://img.example/a.jpg", 0), (20, "https://img.example/b.jpg", 1));
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbidden()
    {
        var hotel = TestData.Hotel(Guid.NewGuid());
        _hotels.Setup(r => r.GetByIdWithImagesTrackedAsync(hotel.Id, It.IsAny<CancellationToken>())).ReturnsAsync(hotel);
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(false);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetHotelImagesQuery(hotel.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_UnknownHotel_ThrowsNotFound()
    {
        _hotels.Setup(r => r.GetByIdWithImagesTrackedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Hotel?)null);

        var act = () => CreateHandler().Handle(new GetHotelImagesQuery(9), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
