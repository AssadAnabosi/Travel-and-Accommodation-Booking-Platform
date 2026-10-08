using Domain.Common;

namespace Domain.Entities;

public class City : AuditableEntity<int>
{
    public string Name { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string PostOffice { get; private set; } = null!;

    public string? ThumbnailUrl { get; private set; }

    private readonly List<Hotel> _hotels = new();
    public IReadOnlyCollection<Hotel> Hotels => _hotels.AsReadOnly();

    protected City()
    {
    } // EF Core

    private City(string name, string country, string postOffice, string? thumbnailUrl)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Country = Guard.AgainstNullOrWhiteSpace(country, nameof(country));
        PostOffice = Guard.AgainstNullOrWhiteSpace(postOffice, nameof(postOffice));
        ThumbnailUrl = NormalizeUrl(thumbnailUrl);
        CreatedAt = DateTime.UtcNow;
    }

    public static City Create(string name, string country, string postOffice, string? thumbnailUrl = null) =>
        new(name, country, postOffice, thumbnailUrl);

    /// <summary>Full replace: a null/blank <paramref name="thumbnailUrl"/> clears the thumbnail.</summary>
    public void Update(string name, string country, string postOffice, string? thumbnailUrl = null)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Country = Guard.AgainstNullOrWhiteSpace(country, nameof(country));
        PostOffice = Guard.AgainstNullOrWhiteSpace(postOffice, nameof(postOffice));
        ThumbnailUrl = NormalizeUrl(thumbnailUrl);
        ModifiedAt = DateTime.UtcNow;
    }

    private static string? NormalizeUrl(string? url) => string.IsNullOrWhiteSpace(url) ? null : url.Trim();
}