using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Hotels.Queries.GetPendingHotels;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetPendingHotels;

public class GetPendingHotelsQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();

    [Fact]
    public async Task Handle_PassesPagingAndMapsHotels()
    {
        var ownerId = Guid.NewGuid();
        var pending = TestData.Hotel(ownerId, approved: false, name: "Draft", id: 7);
        TestData.RoomIn(pending, id: 1);
        TestData.RoomIn(pending, id: 2);
        _hotels.Setup(h => h.GetPendingApprovalAsync(2, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Hotel>([pending], totalCount: 6, pageNumber: 2, pageSize: 5));

        var page = await new GetPendingHotelsQueryHandler(_hotels.Object)
            .Handle(new GetPendingHotelsQuery(PageNumber: 2, PageSize: 5), CancellationToken.None);

        page.PageNumber.Should().Be(2);
        page.TotalPages.Should().Be(2);
        var dto = page.Items.Should().ContainSingle().Subject;
        dto.Id.Should().Be(7);
        dto.ApprovalStatus.Should().Be("Pending");
        dto.OwnerId.Should().Be(ownerId);
        dto.OwnerName.Should().Be("Olivia Owner");
        // Regression (decision #57c): Rooms are included so the count is accurate.
        dto.RoomsCount.Should().Be(2);
    }
}
