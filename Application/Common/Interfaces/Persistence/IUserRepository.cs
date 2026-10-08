using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    // Must eager-load RefreshTokens — needed to find the matching active token and rotate it.
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task<PaginatedList<User>> SearchAsync(UserSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);
}