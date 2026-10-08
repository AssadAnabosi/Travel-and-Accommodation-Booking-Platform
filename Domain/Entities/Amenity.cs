using Domain.Common;

namespace Domain.Entities;

public class Amenity : AuditableEntity<int>
{
    public string Name { get; private set; } = null!;

    private readonly List<HotelAmenity> _hotelAmenities = new();
    public IReadOnlyCollection<HotelAmenity> HotelAmenities => _hotelAmenities.AsReadOnly();

    protected Amenity() { } // EF Core

    private Amenity(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        CreatedAt = DateTime.UtcNow;
    }

    public static Amenity Create(string name) => new(name);

    public void Rename(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        ModifiedAt = DateTime.UtcNow;
    }
}