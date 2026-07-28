namespace DailyMart.Application.Billing;

/// <summary>
/// Platform-admin action: emails one tenant a reminder appropriate to its current billing status -
/// an overdue-payment nudge if its subscription is overdue, or an upgrade-from-Free nudge if it's on the
/// Free plan (never both; a Free-plan tenant is never overdue - see SubscriptionService's IsOverdue
/// calculation). Manually triggered per company from the platform panel (CLAUDE.md's Billing bullet) -
/// no scheduler, no automatic/recurring sends, same "manual-tracking-only" spirit as the rest of billing.
/// </summary>
public interface ITenantReminderEmailService
{
    /// <summary>Throws BusinessRuleException if email sending isn't configured, if the tenant is neither
    /// overdue nor on the Free plan (nothing to remind them about), or if it has no contact email on
    /// file yet (ShopSettings.ShopEmail is optional and not populated at signup).</summary>
    Task<TenantReminderEmailResultDto> SendReminderAsync(long tenantId, CancellationToken cancellationToken = default);
}
