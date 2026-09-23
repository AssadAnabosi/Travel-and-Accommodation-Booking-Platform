using Application.Common.Interfaces.Persistence;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookingRepository(AppDbContext context) : IBookingRepository
{
    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task AddAsync(Booking entity, CancellationToken cancellationToken = default) =>
        await context.Bookings.AddAsync(entity, cancellationToken);

    public void Update(Booking entity) => context.Bookings.Update(entity);

    public void Remove(Booking entity) => context.Bookings.Remove(entity);

    // Tracked (check-in/check-out mutate the booking) with the graph needed for ownership + display.
    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.Bookings
            .Include(b => b.User)
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<PaginatedList<Booking>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Room).ThenInclude(r => r.Hotel)
            .OrderByDescending(b => b.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Booking>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<Booking>> GetByHotelIdAsync(int hotelId, HotelBookingFilter filter,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.Bookings
            .AsNoTracking()
            .Where(b => b.Room.HotelId == hotelId);

        if (filter.Status.HasValue)
            query = query.Where(b => b.Status == filter.Status.Value);

        if (filter.CheckInFrom.HasValue)
            query = query.Where(b => b.StayRange.StartDate >= filter.CheckInFrom.Value);

        if (filter.CheckInTo.HasValue)
            query = query.Where(b => b.StayRange.StartDate <= filter.CheckInTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(b => b.ConfirmationNumber.Contains(filter.Keyword)
                                     || b.User.Email.Contains(filter.Keyword)
                                     || b.User.FirstName.Contains(filter.Keyword)
                                     || b.User.LastName.Contains(filter.Keyword));

        var ordered = query
            .Include(b => b.User)
            .Include(b => b.Room)
            .OrderBy(b => b.StayRange.StartDate)
            .ThenBy(b => b.Room.Number)
            .ThenBy(b => b.CreatedAt);

        var totalCount = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<Booking>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> HasCompletedStayAsync(Guid userId, int hotelId,
        CancellationToken cancellationToken = default) =>
        await context.Bookings.AnyAsync(
            b => b.UserId == userId && b.Status == BookingStatus.CheckedOut && b.Room.HotelId == hotelId,
            cancellationToken);
}