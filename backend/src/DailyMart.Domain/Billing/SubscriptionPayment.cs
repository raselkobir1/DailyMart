using DailyMart.Domain.Common;

namespace DailyMart.Domain.Billing;

/// <summary>
/// Append-only payment ledger for a Tenant's subscription - same "never overwritten" rule as Customer/
/// Supplier payment history (CLAUDE.md §8). PlanId snapshots which plan was being paid for at the time,
/// since a tenant can change plans later. No separate "recorded by" column - AuditableEntity.CreatedBy
/// already carries the platform admin's username via the existing audit interceptor (see
/// ISubscriptionService's doc comment), and every row also gets a free AuditLog entry the same way.
/// PeriodStart/PeriodEnd is the coverage window this one payment buys; ISubscriptionService keeps
/// TenantSubscription.CurrentPeriodEnd reconciled to the latest payment's PeriodEnd (mirrors the
/// Supplier Due "recompute-and-compare" rule, CLAUDE.md §8).
/// </summary>
public class SubscriptionPayment : AuditableEntity
{
    public long TenantId { get; set; }

    public long PlanId { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset PeriodStart { get; set; }

    public DateTimeOffset PeriodEnd { get; set; }

    public string Method { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
