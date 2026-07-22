using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auth;

namespace DailyMart.Application.Auth;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Revokes every currently-active token for a user (logout, password change). Callers still
    /// need to call IUnitOfWork.SaveChangesAsync() afterward - this only stages the changes.</summary>
    Task RevokeAllActiveForUserAsync(long userId, CancellationToken cancellationToken = default);
}
