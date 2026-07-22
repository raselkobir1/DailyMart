using DailyMart.Domain.Common;

namespace DailyMart.Domain.Auth;

/// <summary>
/// An opaque refresh token issued to a User. Only the SHA-256 hash is persisted (see TokenHash) so a
/// database leak alone can't be exchanged for a working session.
/// </summary>
public class RefreshToken : AuditableEntity
{
    public long UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
