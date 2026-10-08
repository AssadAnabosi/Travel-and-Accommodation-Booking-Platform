using Domain.Common;

namespace Domain.Entities;

public class Hotel : AuditableEntity<int>
{
    public string Name { get; private set; } = null!;
    public int StarRating { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public int CityId { get; private set; }
    public City City { get; private set; } = null!;

    public Guid OwnerId { get; private set; }
    public User Owner { get; private set; } = null!;

    private readonly List<Room> _rooms = new();
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private readonly List<Review> _reviews = new();
    public IReadOnlyCollection<Review> Reviews => _reviews.AsReadOnly();

    private readonly List<HotelAmenity> _hotelAmenities = new();
    public IReadOnlyCollection<HotelAmenity> HotelAmenities => _hotelAmenities.AsReadOnly();

    protected Hotel() { } // EF Core

    private Hotel(string name, int starRating, string description, int cityId, Guid ownerId)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        CityId = cityId;
        OwnerId = ownerId;
        CreatedAt = DateTime.UtcNow;
    }

    public static Hotel Create(string name, int starRating, string description, int cityId, Guid ownerId) =>
        new(name, starRating, description, cityId, ownerId);

    public void Update(string name, int starRating, string description, int cityId)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        StarRating = ValidateStarRating(starRating);
        Description = description ?? string.Empty;
        CityId = cityId;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ReassignOwner(Guid newOwnerId)
    {
        OwnerId = newOwnerId;
        ModifiedAt = DateTime.UtcNow;
    }

    public double AverageRating() =>
        _reviews.Count == 0 ? 0 : _reviews.Average(r => r.Rating);

    private static int ValidateStarRating(int starRating)
    {
        if (starRating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(starRating), "Star rating must be between 1 and 5.");
        return starRating;
    }
}