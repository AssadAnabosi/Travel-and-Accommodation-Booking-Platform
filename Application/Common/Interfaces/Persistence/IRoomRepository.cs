using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Common.Interfaces.Persistence;

public interface IRoomRepository : IRepository<Room, int>
{
    // Includes Discounts + Availabilities loaded — required for GetActivePrice() and Reserve() to work correctly in-memory.
    Task<Room?> GetByIdForBookingAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(int roomId, DateRange range, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetByHotelIdAsync(int hotelId, CancellationToken cancellationToken = default);
}