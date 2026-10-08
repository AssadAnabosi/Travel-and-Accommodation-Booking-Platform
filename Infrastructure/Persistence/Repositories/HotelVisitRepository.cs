using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class HotelVisitRepository(AppDbContext context) : IHotelVisitRepository
{
    // Append-only: just stage the row; the handler commits it through IUnitOfWork.
    public async Task RecordAsync(HotelVisit visit, CancellationToken cancellationToken = default) =>
        await context.HotelVisits.AddAsync(visit, cancellationToken);

    public async Task<IReadOnlyList<Hotel>> GetRecentlyVisitedAsync(Guid userId, int count,
        CancellationToken cancellationToken = default)
    {
        // Most-recently-visited distinct hotels: collapse the visit log to one row per hotel (its latest
        // visit), then load those hotels with the graph the DTO projection needs.
        var hotelIds = await context.HotelVisits
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .GroupBy(v => v.HotelId)
            .Select(g => new { HotelId = g.Key, LastVisitedAt = g.Max(v => v.VisitedAt) })
            .OrderByDescending(x => x.LastVisitedAt)
            .Take(count)
            .Select(x => x.HotelId)
            .ToListAsync(cancellationToken);

        var hotels = await context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Where(h => hotelIds.Contains(h.Id))
            .Include(h => h.City)
            .Include(h => h.Images)
            .Include(h => h.Rooms).ThenInclude(r => r.Discounts)
            .ToListAsync(cancellationToken);

        // Restore the recency order the grouping query established (the IN-based load doesn't preserve it).
        return hotelIds
            .Select(id => hotels.First(h => h.Id == id))
            .ToList();
    }

    public async Task<IReadOnlyList<TrendingCity>> GetTrendingCitiesAsync(int count,
        CancellationToken cancellationToken = default) =>
        await context.HotelVisits
            .AsNoTracking()
            .GroupBy(v => new { v.Hotel.CityId, v.Hotel.City.Name })
            .Select(g => new TrendingCity(g.Key.CityId, g.Key.Name, g.Count()))
            .OrderByDescending(c => c.VisitCount)
            .Take(count)
            .ToListAsync(cancellationToken);
}