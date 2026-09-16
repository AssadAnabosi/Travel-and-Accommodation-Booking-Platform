using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Common.Interfaces.Persistence;

public interface IRoomRepository : IRepository<Room, int>
{
    // Includes Discounts + Availabilities loaded — required for GetActivePrice() and Reserve() to work correctly in-memory.
    Task<Room?> GetByIdForBookingAsync(int id, CancellationToken cancellationToken = default);

    // Includes Hotel (ownership checks) + Availabilities + Discounts — used by all admin/owner mutations.
    Task<Room?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(int roomId, DateRange range, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default);

    Task<bool> NumberExistsInHotelAsync(int hotelId, string number, int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> HasFutureBookingsAsync(int roomId, DateOnly asOfDate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Any booking ever, past or future — this is now the deletion criterion, not just upcoming ones.
    /// </summary>
    Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default);
}