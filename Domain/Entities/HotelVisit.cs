using Domain.Common;

namespace Domain.Entities;

/// <summary>
/// Analytics log entry: a user viewed a hotel's detail page.
/// Powers "Recently Visited" and "Trending Destinations" — append-only, never updated.
/// </summary>
public class HotelVisit : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = null!;

    public DateTime VisitedAt { get; private set; }

    protected HotelVisit()
    {
    } // EF Core

    private HotelVisit(Guid userId, int hotelId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        HotelId = hotelId;
        VisitedAt = DateTime.UtcNow;
    }

    public static HotelVisit Record(Guid userId, int hotelId) => new(userId, hotelId);
}