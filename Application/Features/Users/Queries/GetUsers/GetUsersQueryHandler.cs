using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using MediatR;

namespace Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetUsersQuery, PaginatedList<UserListItemDto>>
{
    public async Task<PaginatedList<UserListItemDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var filter = new UserSearchFilter(request.Keyword, request.Role, request.IsActive);
        var result = await userRepository.SearchAsync(filter, request.PageNumber, request.PageSize, cancellationToken);

        var items = result.Items
            .Select(u =>
                new UserListItemDto(u.Id, u.Email, u.FirstName, u.LastName, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToList();

        return new PaginatedList<UserListItemDto>(items, result.TotalCount, result.PageNumber, request.PageSize);
    }
}