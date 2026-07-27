using DailyMart.Domain.Common;

namespace DailyMart.Domain.Billing;

/// <summary>
/// One row per Tenant (unique on TenantId) recording which Plan it's on and, for a paid plan, how far
/// its payment covers it. Global/unscoped like Tenant itself - a platform admin needs to read/join this
/// across every tenant with no tenant JWT context. CurrentPeriodEnd is null both for a Free-plan tenant
/// (never expires) and for a paid-plan tenant with no payment recorded yet - see ISubscriptionService's
/// IsOverdue calculation, which treats both the same way (nothing currently covers them).
/// </summary>
public class TenantSubscription : AuditableEntity
{
    public long TenantId { get; set; }

    public long PlanId { get; set; }

    public DateTimeOffset CurrentPeriodStart { get; set; }

    public DateTimeOffset? CurrentPeriodEnd { get; set; }
}
