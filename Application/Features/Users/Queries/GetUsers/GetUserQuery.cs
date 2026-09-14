using Application.Common.Models;
using Application.Common.Security;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Queries.GetUsers;

[Authorize(Roles = "Admin")]
public record GetUsersQuery(
    string? Keyword,
    UserRole? Role,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<UserListItemDto>>;