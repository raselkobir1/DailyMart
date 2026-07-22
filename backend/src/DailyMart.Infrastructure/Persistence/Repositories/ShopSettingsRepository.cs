using DailyMart.Application.Settings;
using DailyMart.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class ShopSettingsRepository : Repository<ShopSettings>, IShopSettingsRepository
{
    public ShopSettingsRepository(DbContext context) : base(context)
    {
    }

    public Task<ShopSettings?> GetSingletonAsync(CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(cancellationToken);
}
