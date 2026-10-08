using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

// deliberately not extending IRepository<,>: it's an append-only log with specialized read queries, not a CRUD aggregate.
public interface IHotelVisitRepository
{
    Task RecordAsync(HotelVisit visit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Hotel>> GetRecentlyVisitedAsync(Guid userId, int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrendingCity>> GetTrendingCitiesAsync(int count, CancellationToken cancellationToken = default);
}

public record TrendingCity(int CityId, string CityName, string? ThumbnailUrl, int VisitCount);