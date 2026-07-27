namespace DailyMart.Application.UsageAnalytics;

public class TenantUsageSnapshotDto
{
    public long TenantId { get; init; }

    public int TotalUsers { get; init; }

    public int ActiveUsers { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    /// <summary>Most recent AuditLog.PerformedAt for this tenant - an approximation of "activity" (only
    /// create/update/delete/sale actions are audited, not every page view), not a true "last seen."</summary>
    public DateTimeOffset? LastActivityAt { get; init; }

    public int ProductCount { get; init; }

    public int CustomerCount { get; init; }

    public int SupplierCount { get; init; }

    public int SaleCount { get; init; }

    public int PurchaseCount { get; init; }
}
