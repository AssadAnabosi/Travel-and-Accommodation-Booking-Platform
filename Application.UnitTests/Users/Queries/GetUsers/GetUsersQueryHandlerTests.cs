using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Application.Features.Users.Queries.GetUsers;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace Application.UnitTests.Users.Queries.GetUsers;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _users = new();

    [Fact]
    public async Task Handle_PassesFilterAndPagingAndMapsItems()
    {
        var user = User.Create("c@tabp.dev", "hash", "Charlie", "Customer");
        UserSearchFilter? filter = null;
        _users.Setup(u => u.SearchAsync(It.IsAny<UserSearchFilter>(), 3, 15, It.IsAny<CancellationToken>()))
            .Callback<UserSearchFilter, int, int, CancellationToken>((f, _, _, _) => filter = f)
            .ReturnsAsync(new PaginatedList<User>([user], totalCount: 31, pageNumber: 3, pageSize: 15));

        var page = await new GetUsersQueryHandler(_users.Object)
            .Handle(new GetUsersQuery("char", UserRole.Customer, true, PageNumber: 3, PageSize: 15),
                CancellationToken.None);

        filter.Should().Be(new UserSearchFilter("char", UserRole.Customer, true));
        page.TotalPages.Should().Be(3);
        var item = page.Items.Should().ContainSingle().Subject;
        item.Id.Should().Be(user.Id);
        item.Email.Should().Be("c@tabp.dev");
        item.Role.Should().Be("Customer");
        item.IsActive.Should().BeTrue();
    }
}
