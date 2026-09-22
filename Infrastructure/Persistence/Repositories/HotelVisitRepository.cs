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
        CancellationToken cancellationToken = default)
    {
        // Project the city id (via the hotel) to a scalar first so GROUP BY translates to SQL —
        // grouping directly by the nested navigation key (Hotel.CityId + Hotel.City.Name) can't be
        // translated. Resolve the city names in a second small query.
        var counts = await context.HotelVisits
            .AsNoTracking()
            .Select(v => v.Hotel.CityId)
            .GroupBy(cityId => cityId)
            .Select(g => new { CityId = g.Key, VisitCount = g.Count() })
            .OrderByDescending(x => x.VisitCount)
            .Take(count)
            .ToListAsync(cancellationToken);

        if (counts.Count == 0)
            return [];

        var cityIds = counts.Select(x => x.CityId).ToList();
        var namesById = await context.Cities
            .AsNoTracking()
            .Where(c => cityIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return counts
            .Select(x => new TrendingCity(x.CityId, namesById[x.CityId], x.VisitCount))
            .ToList();
    }
}