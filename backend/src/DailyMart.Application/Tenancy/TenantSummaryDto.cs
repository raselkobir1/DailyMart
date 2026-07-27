namespace DailyMart.Application.Tenancy;

public class TenantSummaryDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Null only if the tenant somehow has no TenantSubscription row - shouldn't happen in
    /// practice (PlanSeeder backfills every tenant), but PlatformTenantService can't assume otherwise.</summary>
    public string? PlanName { get; init; }

    public bool IsFree { get; init; }

    public DateTimeOffset? CurrentPeriodEnd { get; init; }

    public bool IsOverdue { get; init; }

    public int TotalUsers { get; init; }

    public int ActiveUsers { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    /// <summary>See TenantUsageSnapshotDto's doc comment - an approximation of activity, not a true
    /// "last seen." Full per-module record counts stay on the detail-page-only usage endpoint.</summary>
    public DateTimeOffset? LastActivityAt { get; init; }

    /// <summary>The more recent of LastLoginAt/LastActivityAt, computed once here rather than
    /// separately on both the frontend and in PlatformTenantService's sort logic - null only if the
    /// tenant has never logged in AND has no audited activity.</summary>
    public DateTimeOffset? LastActiveAt { get; init; }
}
