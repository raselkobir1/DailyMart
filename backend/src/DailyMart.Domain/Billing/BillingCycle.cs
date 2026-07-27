namespace DailyMart.Domain.Billing;

/// <summary>Ignored entirely when Plan.IsFree - a free plan never expires, so it has no cycle to speak of.</summary>
public enum BillingCycle
{
    Monthly,
    Yearly
}
