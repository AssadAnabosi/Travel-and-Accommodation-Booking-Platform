using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Features.Users.Queries.GetUserById;
using Application.UnitTests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Queries.GetUserById;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    [Fact]
    public async Task Handle_ExistingUser_MapsDetailsWithOwnedHotelAndBookingCounts()
    {
        var user = User.Create("owner@tabp.dev", "hash", "Olivia", "Owner", UserRole.HotelOwner);
        var room = TestData.RoomIn(TestData.Hotel(user.Id));
        user.WithItems("_ownedHotels", room.Hotel, TestData.Hotel(user.Id, id: 2))
            .WithItems("_bookings", TestData.BookingFor(user, room));
        user.Deactivate();
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var dto = await new GetUserByIdQueryHandler(_users.Object)
            .Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        dto.Id.Should().Be(user.Id);
        dto.Email.Should().Be("owner@tabp.dev");
        dto.Role.Should().Be("HotelOwner");
        dto.IsActive.Should().BeFalse();
        dto.OwnedHotelsCount.Should().Be(2);
        dto.BookingsCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        var act = () => new GetUserByIdQueryHandler(_users.Object)
            .Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
