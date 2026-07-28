using DailyMart.Domain.Common;

namespace DailyMart.Domain.Rbac;

/// <summary>
/// One row per (Tenant, Menu) pair the platform admin has explicitly opted a tenant into, for a Menu
/// whose <see cref="Menu.IsGenerallyAvailable"/> is false. Global/unscoped like Menu/Tenant itself - a
/// platform admin grants/revokes this with no tenant JWT context, and IFeatureEntitlementService reads
/// it the same way SubscriptionService reads TenantSubscription. Presence of a non-deleted row is the
/// grant; revoking one soft-deletes it (preserving who granted/revoked it and when via the audit
/// columns) rather than adding a separate IsEnabled flag.
/// </summary>
public class TenantFeatureGrant : AuditableEntity
{
    public long TenantId { get; set; }

    public long MenuId { get; set; }
}
