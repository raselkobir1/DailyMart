namespace DailyMart.Application.Billing;

public class TenantSubscriptionDto
{
    public long TenantId { get; init; }

    public string TenantName { get; init; } = string.Empty;

    public long PlanId { get; init; }

    public string PlanName { get; init; } = string.Empty;

    public bool IsFree { get; init; }

    public decimal Price { get; init; }

    public DateTimeOffset CurrentPeriodStart { get; init; }

    public DateTimeOffset? CurrentPeriodEnd { get; init; }

    /// <summary>!IsFree && (CurrentPeriodEnd is null or in the past) - see ISubscriptionService's doc
    /// comment. Never stored, always recomputed at read time.</summary>
    public bool IsOverdue { get; init; }
}
