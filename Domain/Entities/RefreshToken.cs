using Domain.Common;

namespace Domain.Entities;

public class RefreshToken : BaseEntity<Guid>
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public string Token { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; } // token-rotation chain, for audit/reuse-detection

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;

    protected RefreshToken() { } // EF Core

    private RefreshToken(Guid userId, string token, DateTime expiresAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Token = Guard.AgainstNullOrWhiteSpace(token, nameof(token));
        ExpiresAt = expiresAt;
        CreatedAt = DateTime.UtcNow;
    }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt) =>
        new(userId, token, expiresAt);

    public void Revoke(string? replacedByToken = null)
    {
        if (IsRevoked)
            throw new InvalidOperationException("Refresh token has already been revoked.");

        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByToken;
    }
}