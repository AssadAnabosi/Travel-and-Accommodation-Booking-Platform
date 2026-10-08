using Domain.Common;
using Domain.Enums;
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

    protected Room() { } // EF Core

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

    public static Room Create(int hotelId, string number, RoomType roomType, int adultCapacity, int childCapacity, Money basePrice) =>
        new(hotelId, number, roomType, adultCapacity, childCapacity, basePrice);

    public void Update(string number, int adultCapacity, int childCapacity)
    {
        Number = Guard.AgainstNullOrWhiteSpace(number, nameof(number));
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
        var activeDiscount = _discounts.FirstOrDefault(d => d.IsActiveOn(onDate));
        return activeDiscount is null ? BasePrice : activeDiscount.ApplyTo(BasePrice);
    }
}