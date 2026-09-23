using Application.Common.Exceptions;
using Application.Common.Interfaces.Persistence;
using Application.Common.Interfaces.Services;
using Application.Features.Users.Commands.UpdateProfile;
using Application.Features.Users.Queries.GetMyProfile;
using Domain.Entities;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();

    private UpdateProfileCommandHandler CreateHandler() => new(_users.Object, _uow.Object, _currentUser.Object);

    [Fact]
    public async Task Handle_UpdatesTheCurrentUsersNameAndReturnsProfile()
    {
        var user = User.Create("ada@tabp.dev", "hash", "Ada", "Lovelace");
        _currentUser.Setup(c => c.UserId).Returns(user.Id);
        _users.Setup(u => u.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var dto = await CreateHandler().Handle(new UpdateProfileCommand("Augusta", "King"), CancellationToken.None);

        dto.Should().Be(new UserProfileDto(user.Id, "ada@tabp.dev", "Augusta", "King", "Customer"));
        _users.Verify(u => u.Update(user), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNoLongerExists_ThrowsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());

        var act = () => CreateHandler().Handle(new UpdateProfileCommand("A", "B"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
