using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Hotels.Queries.GetHotelById;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetHotelById;

public class GetHotelByIdQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public GetHotelByIdQueryHandlerTests()
    {
        var pending = TestData.Hotel(_ownerId, approved: false, name: "Grand", id: 1, cityName: "Paris");
        TestData.RoomIn(pending, id: 1);
        TestData.RoomIn(pending, id: 2);
        _hotels.Setup(h => h.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pending);
    }

    private GetHotelByIdQueryHandler CreateHandler() => new(_hotels.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_Owner_SeesOwnPendingHotelWithNavigationsMapped()
    {
        _currentUser.Setup(c => c.UserId).Returns(_ownerId);

        var dto = await CreateHandler().Handle(new GetHotelByIdQuery(1), CancellationToken.None);

        dto.Id.Should().Be(1);
        dto.Name.Should().Be("Grand");
        dto.ApprovalStatus.Should().Be("Pending");
        // Regression (decision #57): City/Owner come from the detailed load and must not be null.
        dto.CityName.Should().Be("Paris");
        dto.OwnerId.Should().Be(_ownerId);
        dto.OwnerName.Should().Be("Olivia Owner");
        dto.RoomsCount.Should().Be(2);
        _hotels.Verify(h => h.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_IsAllowed()
    {
        _currentUser.Setup(c => c.IsInRole("Admin")).Returns(true);
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var dto = await CreateHandler().Handle(new GetHotelByIdQuery(1), CancellationToken.None);

        dto.Id.Should().Be(1);
    }

    [Fact]
    public async Task Handle_OtherOwner_ThrowsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new GetHotelByIdQuery(1), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_HotelNotFound_ThrowsNotFound()
    {
        var act = () => CreateHandler().Handle(new GetHotelByIdQuery(99), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
