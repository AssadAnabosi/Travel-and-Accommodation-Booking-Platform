using Domain.Common;

namespace Domain.Entities;

public class RoomImage : BaseEntity<int>
{
    public int RoomId { get; private set; }
    public Room Room { get; private set; } = null!;

    public string Url { get; private set; } = null!;
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected RoomImage()
    {
    } // EF Core

    private RoomImage(int roomId, string url, int displayOrder)
    {
        RoomId = roomId;
        Url = Guard.AgainstNullOrWhiteSpace(url, nameof(url));
        DisplayOrder = displayOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public static RoomImage Create(int roomId, string url, int displayOrder) => new(roomId, url, displayOrder);
}