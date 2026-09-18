using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default) =>
        await context.Users.AddAsync(entity, cancellationToken);

    public void Update(User entity) => context.Users.Update(entity);

    public void Remove(User entity) => context.Users.Remove(entity);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    // Tracked + RefreshTokens loaded so the handler can rotate the matching token in place.
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default) =>
        await context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken), cancellationToken);

    public async Task<PaginatedList<User>> SearchAsync(UserSearchFilter filter, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(u => u.Email.Contains(filter.Keyword)
                                     || u.FirstName.Contains(filter.Keyword)
                                     || u.LastName.Contains(filter.Keyword));

        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(u => u.IsActive == filter.IsActive.Value);

        query = query.OrderBy(u => u.Email);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<User>(items, totalCount, pageNumber, pageSize);
    }
}