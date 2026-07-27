namespace DailyMart.Application.Billing;

public class SubscriptionPaymentDto
{
    public long Id { get; init; }

    public decimal Amount { get; init; }

    public DateTimeOffset PeriodStart { get; init; }

    public DateTimeOffset PeriodEnd { get; init; }

    public string Method { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>The platform admin's username - stamped for free by AuditingSaveChangesInterceptor, see
    /// SubscriptionPayment's doc comment.</summary>
    public string CreatedBy { get; init; } = string.Empty;
}
