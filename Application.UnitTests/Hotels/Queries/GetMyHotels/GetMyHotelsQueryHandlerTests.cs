using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Common.Models;
using Application.Features.Hotels.Queries.GetMyHotels;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetMyHotels;

public class GetMyHotelsQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_QueriesByCurrentUserAndMapsEveryApprovalStatus()
    {
        var ownerId = Guid.NewGuid();
        _currentUser.Setup(c => c.UserId).Returns(ownerId);
        var approved = TestData.Hotel(ownerId, name: "Live", id: 1);
        TestData.RoomIn(approved);
        var pending = TestData.Hotel(ownerId, approved: false, name: "Draft", id: 2);
        _hotels.Setup(h => h.GetByOwnerIdAsync(ownerId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Hotel>([approved, pending], totalCount: 2, pageNumber: 1, pageSize: 20));

        var page = await new GetMyHotelsQueryHandler(_hotels.Object, _currentUser.Object)
            .Handle(new GetMyHotelsQuery(), CancellationToken.None);

        page.TotalCount.Should().Be(2);
        page.Items.Select(h => (h.Name, h.ApprovalStatus, h.RoomsCount))
            .Should().Equal(("Live", "Approved", 1), ("Draft", "Pending", 0));
        // Regression (decision #57): Owner is eager-loaded by GetByOwnerIdAsync.
        page.Items.Should().OnlyContain(h => h.OwnerName == "Olivia Owner" && h.CityName == "Paris");
    }
}
