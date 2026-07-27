using DailyMart.Domain.Billing;

namespace DailyMart.Application.Billing;

/// <summary>Used for both create and update - the writable shape is identical either way. IsActive is
/// deliberately not here, same as Tenant's own activate/deactivate-as-separate-actions convention (see
/// PlatformPlansController).</summary>
public class PlanRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public BillingCycle BillingCycle { get; init; }

    public bool IsFree { get; init; }

    public int SortOrder { get; init; }
}
