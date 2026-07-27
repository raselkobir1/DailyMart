using DailyMart.Application.UsageAnalytics;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Common;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Sales;
using DailyMart.Domain.Suppliers;
using DailyMart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.UsageAnalytics;

/// <summary>
/// Lives in Infrastructure and talks to DailyMartDbContext directly, not through IUnitOfWork/
/// IRepository&lt;T&gt; - same reason as TenantProvisioningService: every entity here (Product, Customer,
/// Supplier, Sale, Purchase, User) is TenantOwnedEntity and auto-filtered to the CURRENT request's
/// tenant, which is null for a platform-admin token - so every read needs IgnoreQueryFilters() plus an
/// explicit TenantId predicate to see across every requested tenant at once. AuditLog needs no such
/// bypass - it was never tenant-filtered to begin with (see AuditLogService's doc comment).
/// </summary>
public class UsageAnalyticsService : IUsageAnalyticsService
{
    private readonly DailyMartDbContext _context;

    public UsageAnalyticsService(DailyMartDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<long, TenantUsageSnapshotDto>> GetSnapshotsByTenantIdsAsync(
        IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
    {
        var ids = tenantIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, TenantUsageSnapshotDto>();
        }

        var userStats = await _context.Users.IgnoreQueryFilters()
            .Where(u => !u.IsDeleted && ids.Contains(u.TenantId))
            .GroupBy(u => u.TenantId)
            .Select(g => new
            {
                TenantId = g.Key,
                Total = g.Count(),
                Active = g.Count(u => u.IsActive),
                LastLogin = g.Max(u => u.LastLoginAt)
            })
            .ToDictionaryAsync(x => x.TenantId, cancellationToken);

        var lastActivityByTenant = await _context.AuditLogs
            .Where(a => a.TenantId != null && ids.Contains(a.TenantId.Value))
            .GroupBy(a => a.TenantId!.Value)
            .Select(g => new { TenantId = g.Key, LastActivityAt = g.Max(a => a.PerformedAt) })
            .ToDictionaryAsync(x => x.TenantId, x => x.LastActivityAt, cancellationToken);

        var productCounts = await CountByTenantAsync(_context.Products, ids, cancellationToken);
        var customerCounts = await CountByTenantAsync(_context.Customers, ids, cancellationToken);
        var supplierCounts = await CountByTenantAsync(_context.Suppliers, ids, cancellationToken);
        var saleCounts = await CountByTenantAsync(_context.Sales, ids, cancellationToken);
        var purchaseCounts = await CountByTenantAsync(_context.Purchases, ids, cancellationToken);

        return ids.ToDictionary(id => id, id =>
        {
            var userStat = userStats.GetValueOrDefault(id);
            return new TenantUsageSnapshotDto
            {
                TenantId = id,
                TotalUsers = userStat?.Total ?? 0,
                ActiveUsers = userStat?.Active ?? 0,
                LastLoginAt = userStat?.LastLogin,
                LastActivityAt = lastActivityByTenant.GetValueOrDefault(id),
                ProductCount = productCounts.GetValueOrDefault(id),
                CustomerCount = customerCounts.GetValueOrDefault(id),
                SupplierCount = supplierCounts.GetValueOrDefault(id),
                SaleCount = saleCounts.GetValueOrDefault(id),
                PurchaseCount = purchaseCounts.GetValueOrDefault(id)
            };
        });
    }

    private static async Task<Dictionary<long, int>> CountByTenantAsync<T>(
        DbSet<T> set, List<long> tenantIds, CancellationToken cancellationToken)
        where T : TenantOwnedEntity
    {
        return await set.IgnoreQueryFilters()
            .Where(e => !e.IsDeleted && tenantIds.Contains(e.TenantId))
            .GroupBy(e => e.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);
    }
}
