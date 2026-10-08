using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ReviewRepository(AppDbContext context) : IReviewRepository
{
    public async Task<Review?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Reviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(Review entity, CancellationToken cancellationToken = default) =>
        await context.Reviews.AddAsync(entity, cancellationToken);

    public void Update(Review entity) => context.Reviews.Update(entity);

    public void Remove(Review entity) => context.Reviews.Remove(entity);

    public async Task<PaginatedList<Review>> GetByHotelIdAsync(int hotelId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Reviews
            .AsNoTracking()
            .Where(r => r.HotelId == hotelId)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Review>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> UserHasReviewedAsync(Guid userId, int hotelId,
        CancellationToken cancellationToken = default) =>
        await context.Reviews.AnyAsync(r => r.UserId == userId && r.HotelId == hotelId, cancellationToken);
}