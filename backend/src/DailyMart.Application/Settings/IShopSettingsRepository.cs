using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Settings;

namespace DailyMart.Application.Settings;

public interface IShopSettingsRepository : IRepository<ShopSettings>
{
    /// <summary>Settings is a singleton - callers never need to know or track its Id.</summary>
    Task<ShopSettings?> GetSingletonAsync(CancellationToken cancellationToken = default);
}
