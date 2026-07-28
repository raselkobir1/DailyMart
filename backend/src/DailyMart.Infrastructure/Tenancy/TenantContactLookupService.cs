using DailyMart.Application.Tenancy;
using DailyMart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Tenancy;

/// <summary>
/// Lives in Infrastructure and talks to DailyMartDbContext directly, not through IUnitOfWork/
/// IRepository&lt;T&gt; - same reason as UsageAnalyticsService/TenantProvisioningService: ShopSettings is a
/// TenantOwnedEntity, auto-filtered to the CURRENT request's tenant, which is null for a platform-admin
/// token - so this read needs IgnoreQueryFilters() plus an explicit TenantId predicate to reach one
/// specific tenant's row from platform-admin context.
/// </summary>
public class TenantContactLookupService : ITenantContactLookupService
{
    private readonly DailyMartDbContext _context;

    public TenantContactLookupService(DailyMartDbContext context)
    {
        _context = context;
    }

    public async Task<string?> GetShopEmailAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.ShopSettings.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted && s.TenantId == tenantId)
            .Select(s => s.ShopEmail)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
