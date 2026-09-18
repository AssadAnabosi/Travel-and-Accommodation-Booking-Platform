using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class DiscountRepository(AppDbContext context) : IDiscountRepository
{
    public async Task<Discount?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Discounts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddAsync(Discount entity, CancellationToken cancellationToken = default) =>
        await context.Discounts.AddAsync(entity, cancellationToken);

    public void Update(Discount entity) => context.Discounts.Update(entity);

    public void Remove(Discount entity) => context.Discounts.Remove(entity);

    public async Task<IReadOnlyList<Discount>> GetByRoomIdAsync(int roomId,
        CancellationToken cancellationToken = default) =>
        await context.Discounts
            .AsNoTracking()
            .Where(d => d.RoomId == roomId)
            .OrderBy(d => d.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Discount>> GetActiveByRoomIdAsync(int roomId, DateOnly onDate,
        CancellationToken cancellationToken = default) =>
        await context.Discounts
            .AsNoTracking()
            .Where(d => d.RoomId == roomId && d.IsActive && d.StartDate <= onDate && onDate <= d.EndDate)
            .OrderBy(d => d.StartDate)
            .ToListAsync(cancellationToken);

    // Tracked + Room.Hotel loaded so Update/Deactivate/Delete can run ownership checks without a second query.
    public async Task<Discount?> GetByIdWithRoomAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Discounts
            .Include(d => d.Room).ThenInclude(r => r.Hotel)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<bool> HasOverlappingDiscountAsync(int roomId, DateOnly startDate, DateOnly endDate,
        int? excludeId = null, CancellationToken cancellationToken = default) =>
        await context.Discounts.AnyAsync(
            d => d.RoomId == roomId
                 && d.IsActive
                 && (excludeId == null || d.Id != excludeId)
                 && d.StartDate <= endDate && startDate <= d.EndDate,
            cancellationToken);
}