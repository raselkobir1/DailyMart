using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Billing;

/// <summary>
/// Manages the one TenantSubscription row per tenant and its append-only SubscriptionPayment ledger -
/// the platform-admin side of "who's paid, who's overdue" (CLAUDE.md §4's Billing bullet). Deliberately
/// has no opinion on payment collection itself (no gateway, no webhooks) - every payment here is the
/// platform admin manually recording money already collected outside the app.
/// </summary>
public interface ISubscriptionService
{
    Task<TenantSubscriptionDto> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default);

    Task<PagedResult<SubscriptionPaymentDto>> GetPaymentHistoryAsync(
        long tenantId, PagedRequest request, CancellationToken cancellationToken = default);

    /// <summary>Switching to a Free plan clears CurrentPeriodEnd (never expires). Switching from Free to
    /// a paid plan leaves CurrentPeriodEnd null - which immediately reads as Overdue, prompting a
    /// payment. Switching between two paid plans leaves the existing CurrentPeriodEnd untouched.</summary>
    Task<TenantSubscriptionDto> ChangePlanAsync(long tenantId, long planId, CancellationToken cancellationToken = default);

    /// <summary>Throws BusinessRuleException if the tenant's current plan IsFree - there's nothing to
    /// pay for. PeriodStart continues from the later of the existing CurrentPeriodEnd or today, so
    /// consecutive payments cover back-to-back windows with no gap or overlap.</summary>
    Task<SubscriptionPaymentDto> RecordPaymentAsync(
        long tenantId, RecordPaymentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Batch lookup for the platform tenant list, so it can show Plan/Paid-Until/Overdue per
    /// row without a per-row round trip. Tenants with no subscription row are omitted from the result
    /// (shouldn't happen in practice - PlanSeeder backfills every tenant - but callers should not assume
    /// every requested id comes back).</summary>
    Task<Dictionary<long, TenantSubscriptionDto>> GetSummariesByTenantIdsAsync(
        IEnumerable<long> tenantIds, CancellationToken cancellationToken = default);
}
