using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Room : AuditableEntity<int>
{
    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = null!;

    public string Number { get; private set; } = null!;
    public RoomType RoomType { get; private set; }
    public int AdultCapacity { get; private set; }
    public int ChildCapacity { get; private set; }
    public Money BasePrice { get; private set; } = null!;
    public bool IsActive { get; private set; } = true; // admin can retire without deleting history

    private readonly List<RoomAvailability> _availabilities = new();
    public IReadOnlyCollection<RoomAvailability> Availabilities => _availabilities.AsReadOnly();

    private readonly List<Discount> _discounts = new();
    public IReadOnlyCollection<Discount> Discounts => _discounts.AsReadOnly();

    private readonly List<RoomImage> _images = new();
    public IReadOnlyCollection<RoomImage> Images => _images.AsReadOnly();

    protected Room()
    {
    } // EF Core

    private Room(int hotelId, string number, RoomType roomType, int adultCapacity, int childCapacity, Money basePrice)
    {
        HotelId = hotelId;
        Number = Guard.AgainstNullOrWhiteSpace(number, nameof(number));
        RoomType = roomType;
        AdultCapacity = Guard.AgainstNegativeOrZero(adultCapacity, nameof(adultCapacity));
        ChildCapacity = childCapacity < 0
            ? throw new ArgumentOutOfRangeException(nameof(childCapacity))
            : childCapacity;
        BasePrice = basePrice;
        CreatedAt = DateTime.UtcNow;
    }

    public static Room Create(int hotelId, string number, RoomType roomType, int adultCapacity, int childCapacity,
        Money basePrice) =>
        new(hotelId, number, roomType, adultCapacity, childCapacity, basePrice);

    public void Update(int adultCapacity, int childCapacity)
    {
        AdultCapacity = Guard.AgainstNegativeOrZero(adultCapacity, nameof(adultCapacity));
        ChildCapacity = childCapacity < 0
            ? throw new ArgumentOutOfRangeException(nameof(childCapacity))
            : childCapacity;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ChangeBasePrice(Money newPrice)
    {
        BasePrice = newPrice;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Retire() => IsActive = false;
    public void Reactivate() => IsActive = true;

    /// <summary>True only if no Booked/Blocked RoomAvailability record overlaps the requested range.</summary>
    public bool IsAvailableFor(DateRange range) =>
        IsActive && _availabilities.All(a => !a.Range.Overlaps(range));

    public Money GetActivePrice(DateOnly onDate)
    {
        // Multiple discounts may be active on the same date; apply the one that
        // yields the lowest price for the guest rather than an arbitrary first match.
        return _discounts
            .Where(d => d.IsActiveOn(onDate))
            .Select(d => d.ApplyTo(BasePrice))
            .DefaultIfEmpty(BasePrice)
            .MinBy(price => price.Amount)!;
    }

    public RoomAvailability Reserve(DateRange range, Guid bookingId)
    {
        if (!IsAvailableFor(range))
            throw new RoomNotAvailableException(Id, range);

        var availability = RoomAvailability.ForBooking(Id, range, bookingId);
        _availabilities.Add(availability);
        return availability;
    }

    public RoomAvailability Block(DateRange range)
    {
        if (!IsAvailableFor(range))
            throw new RoomNotAvailableException(Id, range);

        var availability = RoomAvailability.ForBlock(Id, range);
        _availabilities.Add(availability);
        return availability;
    }

    public RoomAvailability? FindAvailability(int availabilityId) =>
        _availabilities.FirstOrDefault(a => a.Id == availabilityId);

    public void Unblock(RoomAvailability availability)
    {
        if (availability.Status != AvailabilityStatus.Blocked)
            throw new InvalidStateTransitionException(
                "Only a manually blocked range can be unblocked directly — bookings must be cancelled instead.");

        _availabilities.Remove(availability);
    }

    public RoomImage AddImage(string url)
    {
        var nextOrder = _images.Count == 0 ? 0 : _images.Max(i => i.DisplayOrder) + 1;
        var image = RoomImage.Create(Id, url, nextOrder);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(int imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId)
                    ?? throw new ImageNotFoundException(imageId, "room");
        _images.Remove(image);
    }

    /// <summary>
    /// Frees the original number for reuse by a genuinely new room, while the mangled
    /// value keeps this row uniquely identifiable in historical Booking/Discount records.
    /// </summary>
    /// <exception cref="InvalidStateTransitionException">The room is already inactive.</exception>
    public void MarkDeleted()
    {
        if (!IsActive)
            throw new InvalidStateTransitionException("Room is already inactive.");

        IsActive = false;
        Number = $"{Number}::deleted::{Guid.NewGuid():N}";
        ModifiedAt = DateTime.UtcNow;
    }
}