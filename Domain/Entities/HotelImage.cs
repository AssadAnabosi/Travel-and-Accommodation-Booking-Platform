using Domain.Common;

namespace Domain.Entities;

public class HotelImage : BaseEntity<int>
{
    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = null!;

    public string Url { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected HotelImage()
    {
    } // EF Core

    private HotelImage(int hotelId, string url, int displayOrder)
    {
        HotelId = hotelId;
        Url = Guard.AgainstNullOrWhiteSpace(url, nameof(url));
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public static HotelImage Create(int hotelId, string url, int displayOrder) => new(hotelId, url, displayOrder);
}