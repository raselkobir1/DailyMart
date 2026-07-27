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
}
