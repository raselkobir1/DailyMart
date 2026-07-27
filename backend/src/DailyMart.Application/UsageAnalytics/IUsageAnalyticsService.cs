namespace DailyMart.Application.UsageAnalytics;

/// <summary>
/// The platform-admin "who's actually using this" snapshot - live-read counts/timestamps from existing
/// tables (Users, AuditLog, business records), not a stored time-series. Distinct from billing
/// (ISubscriptionService): a tenant can be fully paid and still dormant, or on the Free plan and heavily
/// active - this answers a different question than "who owes money."
/// </summary>
public interface IUsageAnalyticsService
{
    /// <summary>Every requested id gets an entry in the result, zero-filled if the tenant has no rows
    /// yet - safe for callers to index without a null-check per id, unlike a lookup that only returns
    /// tenants with existing data.</summary>
    Task<Dictionary<long, TenantUsageSnapshotDto>> GetSnapshotsByTenantIdsAsync(
        IEnumerable<long> tenantIds, CancellationToken cancellationToken = default);
}
