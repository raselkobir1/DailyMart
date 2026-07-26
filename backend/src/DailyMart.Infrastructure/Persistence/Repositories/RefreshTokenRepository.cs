using DailyMart.Application.Auth;
using DailyMart.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(DbContext context) : base(context)
    {
    }

    /// <summary>Bypasses the tenant filter, same reasoning as UserRepository.GetByUsernameAsync: this
    /// runs on the anonymous refresh/logout endpoints, before any tenant context is established - the
    /// token itself is how the caller's identity (and therefore tenant) gets discovered.</summary>
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Entities.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsDeleted, cancellationToken);

    public async Task RevokeAllActiveForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var activeTokens = await Entities
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }
    }
}
