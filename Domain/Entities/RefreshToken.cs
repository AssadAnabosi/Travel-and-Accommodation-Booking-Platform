using Domain.Common;
using Domain.Exceptions;

namespace Domain.Entities;

public class RefreshToken : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    protected RefreshToken() { } // EF Core

    private RefreshToken(Guid userId, string tokenHash, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = Guard.AgainstNullOrWhiteSpace(tokenHash, nameof(tokenHash));
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt) =>
        new(userId, tokenHash, expiresAt);

    public void Revoke()
    {
        if (IsRevoked)
            throw new InvalidStateTransitionException("Refresh token has already been revoked.");

        RevokedAt = DateTime.UtcNow;
    }
}