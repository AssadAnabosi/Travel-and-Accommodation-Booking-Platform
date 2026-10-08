using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    // Must eager-load RefreshTokens — needed to find the matching active token and rotate it.
    Task<User?> GetByRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    // Adds a newly issued refresh token. Kept separate from Update(user) so EF inserts the new
    // token instead of mis-marking its client-generated key as an update to a non-existent row.
    void AddRefreshToken(RefreshToken refreshToken);

    Task<PaginatedList<User>> SearchAsync(UserSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    // Loads a user with its OwnedHotels/Bookings counts via a single projection query,
    // so the collections themselves are never materialized just to be counted.
    Task<UserWithCounts?> GetByIdWithCountsAsync(Guid id, CancellationToken cancellationToken = default);
}