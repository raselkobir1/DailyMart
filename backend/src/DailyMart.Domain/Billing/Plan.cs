using DailyMart.Domain.Common;

namespace DailyMart.Domain.Billing;

/// <summary>
/// A billing tier the platform vendor sells (Free/Basic/Pro/...) - global/unscoped like Tenant/Menu/
/// PlatformAdmin, not TenantOwnedEntity, since the platform admin manages this list across every
/// tenant with no tenant context at all. Deliberately just a billing label: nothing elsewhere in the
/// app reads Plan to gate a feature or enforce a limit - see CLAUDE.md §4's Billing bullet.
/// IsActive retires a plan from being assignable to a tenant going forward without disturbing any
/// tenant already subscribed to it, same convention as Tenant.IsActive.
/// </summary>
public class Plan : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;

    public bool IsFree { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
