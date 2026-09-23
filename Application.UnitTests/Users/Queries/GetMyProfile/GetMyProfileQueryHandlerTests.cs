using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Users.Queries.GetMyProfile;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Queries.GetMyProfile;

public class GetMyProfileQueryHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    [Fact]
    public async Task Handle_LoadsTheCurrentUserAndMapsProfile()
    {
        var user = User.Create("ada@tabp.dev", "hash", "Ada", "Lovelace", UserRole.HotelOwner);
        _currentUser.Setup(c => c.UserId).Returns(user.Id);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var dto = await new GetMyProfileQueryHandler(_users.Object, _currentUser.Object)
            .Handle(new GetMyProfileQuery(), CancellationToken.None);

        dto.Should().Be(new UserProfileDto(user.Id, "ada@tabp.dev", "Ada", "Lovelace", "HotelOwner"));
    }

    [Fact]
    public async Task Handle_UserNoLongerExists_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => new GetMyProfileQueryHandler(_users.Object, _currentUser.Object)
            .Handle(new GetMyProfileQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
