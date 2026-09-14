using Domain.Common;

namespace Domain.Entities;

public class City : AuditableEntity<int>
{
    public string Name { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public string PostOffice { get; private set; } = null!;

    private readonly List<Hotel> _hotels = new();
    public IReadOnlyCollection<Hotel> Hotels => _hotels.AsReadOnly();

    protected City() { } // EF Core

    private City(string name, string country, string postOffice)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Country = Guard.AgainstNullOrWhiteSpace(country, nameof(country));
        PostOffice = Guard.AgainstNullOrWhiteSpace(postOffice, nameof(postOffice));
        CreatedAt = DateTime.UtcNow;
    }

    public static City Create(string name, string country, string postOffice) =>
        new(name, country, postOffice);

    public void Update(string name, string country, string postOffice)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Country = Guard.AgainstNullOrWhiteSpace(country, nameof(country));
        PostOffice = Guard.AgainstNullOrWhiteSpace(postOffice, nameof(postOffice));
        ModifiedAt = DateTime.UtcNow;
    }
}