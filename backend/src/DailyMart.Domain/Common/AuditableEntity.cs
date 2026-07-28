namespace DailyMart.Domain.Common;

/// <summary>
/// Base type for every entity in the system. Carries the shared audit columns
/// (CreatedAt/CreatedBy/UpdatedAt/UpdatedBy/IsDeleted) described in CLAUDE.md §4-5.
/// Inherited directly only by entities that are global/unscoped across every tenant (<see
/// cref="Tenancy.Tenant"/>, <see cref="Tenancy.PlatformAdmin"/>, <see cref="Rbac.Menu"/>, <see
/// cref="Rbac.TenantFeatureGrant"/>, and the Plan/TenantSubscription/SubscriptionPayment billing
/// entities) - every other entity inherits <see cref="TenantOwnedEntity"/> instead, which adds TenantId.
/// </summary>
public abstract class AuditableEntity : IEntity
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
