using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Discount : AuditableEntity<int>
{
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    public string Name { get; private set; } = null!; // e.g. "Summer Featured Deal"
    public DiscountType Type { get; private set; }
    public decimal Value { get; private set; } // percentage (0-100) or fixed amount, per Type

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected Discount() { } // EF Core

    private Discount(int roomId, string name, DiscountType type, decimal value, DateOnly startDate, DateOnly endDate)
    {
        RoomId = roomId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Type = type;
        Value = ValidateValue(type, value);

        if (endDate <= startDate)
            throw new InvalidDateRangeException("Discount end date must be after the start date.");

        StartDate = startDate;
        EndDate = endDate;
        CreatedAt = DateTime.UtcNow;
    }

    public static Discount Create(int roomId, string name, DiscountType type, decimal value, DateOnly startDate, DateOnly endDate) =>
        new(roomId, name, type, value, startDate, endDate);

    public bool IsActiveOn(DateOnly date) =>
        IsActive && date >= StartDate && date <= EndDate;

    public Money ApplyTo(Money price) => Type switch
    {
        DiscountType.Percentage => price.ApplyPercentageDiscount(Value),
        DiscountType.FixedAmount => price.Subtract(Money.Of(Value, price.Currency)),
        _ => price
    };

    public void Deactivate()
    {
        IsActive = false;
        ModifiedAt = DateTime.UtcNow;
    }

    private static decimal ValidateValue(DiscountType type, decimal value)
    {
        if (value <= 0)
            throw new InvalidDiscountException("Discount value must be greater than zero.");
        if (type == DiscountType.Percentage && value > 100)
            throw new InvalidDiscountException("Percentage discount cannot exceed 100.");
        return value;
    }
}