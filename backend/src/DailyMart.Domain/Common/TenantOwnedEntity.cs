namespace DailyMart.Domain.Common;

/// <summary>
/// Base type for every entity that belongs to exactly one tenant (company/shop). Split out from
/// <see cref="AuditableEntity"/> - rather than an explicit exclusion list - so a new entity's author
/// makes the tenant-scoping choice explicitly by which base class they inherit: <see
/// cref="Tenancy.Tenant"/>, <see cref="Tenancy.PlatformAdmin"/>, and <see cref="Rbac.Menu"/> (the
/// shared nav-item list common to every tenant) inherit <see cref="AuditableEntity"/> directly and
/// are never tenant-filtered; everything else inherits this and gets both the soft-delete filter
/// and the tenant-isolation filter for free (see TenancyQueryFilterExtensions).
/// </summary>
public abstract class TenantOwnedEntity : AuditableEntity
{
    public long TenantId { get; set; }
}
