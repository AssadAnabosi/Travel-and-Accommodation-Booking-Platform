using Application.Common.Interfaces.Persistence;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class RoomRepository(AppDbContext context) : IRoomRepository
{
    public async Task<Room?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddAsync(Room entity, CancellationToken cancellationToken = default) =>
        await context.Rooms.AddAsync(entity, cancellationToken);

    public void Update(Room entity) => context.Rooms.Update(entity);

    public void Remove(Room entity) => context.Rooms.Remove(entity);

    // Tracked + Discounts/Availabilities loaded so GetActivePrice() and Reserve() work against the graph.
    public async Task<Room?> GetByIdForBookingAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Rooms
            .Include(r => r.Discounts)
            .Include(r => r.Availabilities)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    // Tracked + Hotel (ownership) and Availabilities/Discounts loaded for admin/owner mutations.
    public async Task<Room?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Rooms
            .Include(r => r.Hotel)
            .Include(r => r.Availabilities)
            .Include(r => r.Discounts)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    // SQL mirror of Room.IsAvailableFor: active room with no Booked/Blocked range overlapping [Start, End).
    public async Task<bool> IsAvailableAsync(int roomId, DateRange range,
        CancellationToken cancellationToken = default)
    {
        var start = range.StartDate;
        var end = range.EndDate;

        return await context.Rooms.AnyAsync(
            r => r.Id == roomId
                 && r.IsActive
                 && !r.Availabilities.Any(a => a.Range.StartDate < end && start < a.Range.EndDate),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetByHotelIdAsync(int hotelId,
        CancellationToken cancellationToken = default) =>
        await context.Rooms
            .AsNoTracking()
            .Where(r => r.HotelId == hotelId)
            .OrderBy(r => r.Number)
            .ToListAsync(cancellationToken);

    public async Task<bool> NumberExistsInHotelAsync(int hotelId, string number, int? excludeId = null,
        CancellationToken cancellationToken = default) =>
        await context.Rooms.AnyAsync(
            r => r.HotelId == hotelId && r.Number == number && (excludeId == null || r.Id != excludeId),
            cancellationToken);

    public async Task<bool> HasFutureBookingsAsync(int roomId, DateOnly asOfDate,
        CancellationToken cancellationToken = default) =>
        await context.Bookings.AnyAsync(
            b => b.RoomId == roomId && b.Status != BookingStatus.Cancelled && b.StayRange.EndDate > asOfDate,
            cancellationToken);

    public async Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await context.Bookings.AnyAsync(b => b.RoomId == roomId, cancellationToken);
}