using Application.Common.Models;
using Domain.Entities;

namespace Application.Common.Interfaces.Persistence;

public interface IBookingRepository : IRepository<Booking, Guid>
{
    //  Must eager-load: User, Room, Room.Hotel — needed for ownership checks and hotel/room display.
    Task<Booking?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaginatedList<Booking>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A hotel's bookings for the owner/admin front desk, ordered by check-in date.
    /// Must eager-load User (guest) and Room (room number).
    /// </summary>
    Task<PaginatedList<Booking>> GetByHotelIdAsync(int hotelId, HotelBookingFilter filter, int pageNumber,
        int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// True if the user has at least one CheckedOut booking for a room belonging to this hotel.
    /// </summary>
    Task<bool> HasCompletedStayAsync(Guid userId, int hotelId, CancellationToken cancellationToken = default);
}