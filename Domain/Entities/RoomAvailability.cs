using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// A date range during which a room is NOT bookable — either held by a Booking,
/// or blocked by an admin/owner (maintenance, etc.). Absence of a record for a
/// given date implies the room is available.
/// </summary>
public class RoomAvailability : AuditableEntity<int>
{
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    public DateRange Range { get; private set; } = null!;
    public AvailabilityStatus Status { get; private set; }

    /// <summary>Set when Status == Booked; null for admin-blocked ranges.</summary>
    public Guid? BookingId { get; private set; }

    protected RoomAvailability() { } // EF Core

    private RoomAvailability(int roomId, DateRange range, AvailabilityStatus status, Guid? bookingId)
    {
        RoomId = roomId;
        Range = range;
        Status = status;
        BookingId = bookingId;
        CreatedAt = DateTime.UtcNow;
    }

    public static RoomAvailability ForBooking(int roomId, DateRange range, Guid bookingId) =>
        new(roomId, range, AvailabilityStatus.Booked, bookingId);

    public static RoomAvailability ForBlock(int roomId, DateRange range) =>
        new(roomId, range, AvailabilityStatus.Blocked, bookingId: null);
}