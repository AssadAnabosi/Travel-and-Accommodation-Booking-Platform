using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IDiscountRepository : IRepository<Discount, int>
{
    Task<IReadOnlyList<Discount>> GetByRoomIdAsync(int roomId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Discount>> GetActiveByRoomIdAsync(int roomId, DateOnly onDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// // Ownership check + Room.Hotel eager-loaded, so Update/Deactivate/Delete don't need a separate room lookup.
    /// </summary>
    Task<Discount?> GetByIdWithRoomAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prevents two active discounts silently competing for the same room/date-range
    /// </summary>
    Task<bool> HasOverlappingDiscountAsync(int roomId, DateOnly startDate, DateOnly endDate, int? excludeId = null,
        CancellationToken cancellationToken = default);
}