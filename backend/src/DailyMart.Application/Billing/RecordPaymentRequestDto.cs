namespace DailyMart.Application.Billing;

public class RecordPaymentRequestDto
{
    public decimal Amount { get; init; }

    /// <summary>The date this payment covers the tenant's subscription through - becomes the new
    /// TenantSubscription.CurrentPeriodEnd (see ISubscriptionService.RecordPaymentAsync).</summary>
    public DateTimeOffset PaidUntil { get; init; }

    public string Method { get; init; } = string.Empty;

    public string? Notes { get; init; }
}
