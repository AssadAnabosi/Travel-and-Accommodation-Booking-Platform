using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Hotels.Queries.GetHotels;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Hotels.Queries.GetHotels;

public class GetHotelsQueryHandlerTests
{
    private readonly Mock<IHotelRepository> _hotels = new();

    [Fact]
    public async Task Handle_PassesEveryFilterAndPagingToTheRepository()
    {
        var ownerId = Guid.NewGuid();
        _hotels.Setup(r => r.GetAllForAdminAsync(It.IsAny<HotelAdminFilter>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedList<Hotel>([], 42, 3, 10));

        var result = await new GetHotelsQueryHandler(_hotels.Object).Handle(
            new GetHotelsQuery("Grand", 7, HotelApprovalStatus.Rejected, ownerId, PageNumber: 3, PageSize: 10),
            CancellationToken.None);

        _hotels.Verify(r => r.GetAllForAdminAsync(
            new HotelAdminFilter("Grand", 7, HotelApprovalStatus.Rejected, ownerId), 3, 10,
            It.IsAny<CancellationToken>()), Times.Once);
        result.TotalCount.Should().Be(42);
        result.PageNumber.Should().Be(3);
        result.Items.Should().BeEmpty();
    }
}
