namespace Domain.Entities;

/// <summary>
/// Join entity for the Hotel-Amenity many-to-many relationship.
/// Configured with a composite key (HotelId, AmenityId) in Infrastructure — no surrogate ID.
/// </summary>
public class HotelAmenity
{
    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = null!;

    public int AmenityId { get; private set; }
    public Amenity Amenity { get; private set; } = null!;

    protected HotelAmenity() { } // EF Core

    private HotelAmenity(int hotelId, int amenityId)
    {
        HotelId = hotelId;
        AmenityId = amenityId;
    }

    public static HotelAmenity Create(int hotelId, int amenityId) => new(hotelId, amenityId);
}