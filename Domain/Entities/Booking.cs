using Domain.Common;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Booking : AuditableEntity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    public DateRange StayRange { get; private set; } = null!;
    public int Adults { get; private set; }
    public int Children { get; private set; }

    public BookingStatus Status { get; private set; }
    public Money TotalPrice { get; private set; } = null!;
    public string? SpecialRequests { get; private set; }

    /// <summary>Customer-facing booking reference</summary>
    public string ConfirmationNumber { get; private set; } = null!;

    protected Booking()
    {
    } // EF Core

    private Booking(Guid userId, int roomId, DateRange stayRange, int adults, int children, Money totalPrice,
        string? specialRequests)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        RoomId = roomId;
        StayRange = stayRange;
        Adults = Guard.AgainstNegativeOrZero(adults, nameof(adults));
        Children = children < 0 ? throw new ArgumentOutOfRangeException(nameof(children)) : children;
        TotalPrice = totalPrice;
        SpecialRequests = specialRequests;
        Status = BookingStatus.Pending;
        ConfirmationNumber = GenerateConfirmationNumber();
        CreatedAt = DateTime.UtcNow;
    }

    public static Booking Create(Guid userId, int roomId, DateRange stayRange, int adults, int children,
        Money totalPrice, string? specialRequests = null) =>
        new(userId, roomId, stayRange, adults, children, totalPrice, specialRequests);

    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm a booking in status {Status}.");
        Status = BookingStatus.Confirmed;
        ModifiedAt = DateTime.UtcNow;
    }

    public void CheckIn()
    {
        if (Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Cannot check in a booking in status {Status}.");
        Status = BookingStatus.CheckedIn;
        ModifiedAt = DateTime.UtcNow;
    }

    public void CheckOut()
    {
        if (Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException($"Cannot check out a booking in status {Status}.");
        Status = BookingStatus.CheckedOut;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is BookingStatus.CheckedOut or BookingStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel a booking in status {Status}.");
        Status = BookingStatus.Cancelled;
        ModifiedAt = DateTime.UtcNow;
    }

    private static string GenerateConfirmationNumber() =>
        $"HB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}