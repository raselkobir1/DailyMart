namespace DailyMart.Application.Tenancy;

/// <summary>
/// Tells the platform owner a new company just signed up (self-service registration) - a side channel,
/// not a business rule: unlike SaleInvoiceDeliveryService/TenantReminderEmailService (explicit,
/// user-triggered sends where a failure should surface to whoever clicked the button), this fires as a
/// side effect of someone ELSE'S registration succeeding, so it must never throw or make a signup fail.
/// See PlatformNotificationService for how that's enforced.
/// </summary>
public interface IPlatformNotificationService
{
    Task NotifyNewTenantSignupAsync(
        long tenantId, string tenantName, string adminUsername, CancellationToken cancellationToken = default);
}
