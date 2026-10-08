using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class User : AuditableEntity<Guid>
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<Hotel> _ownedHotels = new();
    public IReadOnlyCollection<Hotel> OwnedHotels => _ownedHotels.AsReadOnly();

    private readonly List<Booking> _bookings = new();
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    protected User()
    {
    } // EF Core

    private User(Guid id, string email, string passwordHash, string firstName, string lastName, UserRole role)
    {
        Id = id;
        Email = Guard.AgainstNullOrWhiteSpace(email, nameof(email));
        PasswordHash = Guard.AgainstNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    public static User Create(string email, string passwordHash, string firstName, string lastName,
        UserRole role = UserRole.Customer) =>
        new(Guid.NewGuid(), email, passwordHash, firstName, lastName, role);

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = Guard.AgainstNullOrWhiteSpace(firstName, nameof(firstName));
        LastName = Guard.AgainstNullOrWhiteSpace(lastName, nameof(lastName));
        ModifiedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = Guard.AgainstNullOrWhiteSpace(newPasswordHash, nameof(newPasswordHash));
        ModifiedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void PromoteToRole(UserRole role)
    {
        Role = role;
        ModifiedAt = DateTime.UtcNow;
    }
    
    public RefreshToken IssueRefreshToken(string tokenHash, DateTime expiresAt)
    {
        var refreshToken = RefreshToken.Create(Id, tokenHash, expiresAt);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public RefreshToken? FindActiveRefreshToken(string tokenHash) =>
        _refreshTokens.FirstOrDefault(rt => rt.TokenHash == tokenHash && rt.IsActive);
}