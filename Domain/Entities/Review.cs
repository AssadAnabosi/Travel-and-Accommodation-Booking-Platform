using Domain.Common;

namespace Domain.Entities;

public class Review : AuditableEntity<int>
{
    public int HotelId { get; private set; }
    public Hotel Hotel { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public int Rating { get; private set; }
    public string? Comment { get; private set; }

    protected Review() { } // EF Core

    private Review(int hotelId, Guid userId, int rating, string? comment)
    {
        HotelId = hotelId;
        UserId = userId;
        Rating = ValidateRating(rating);
        Comment = comment;
        CreatedAt = DateTime.UtcNow;
    }

    public static Review Create(int hotelId, Guid userId, int rating, string? comment) =>
        new(hotelId, userId, rating, comment);

    public void Edit(int rating, string? comment)
    {
        Rating = ValidateRating(rating);
        Comment = comment;
        ModifiedAt = DateTime.UtcNow;
    }

    private static int ValidateRating(int rating)
    {
        if (rating is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        return rating;
    }
}