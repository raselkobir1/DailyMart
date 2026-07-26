using DailyMart.Domain.Common;

namespace DailyMart.Domain.Tenancy;

/// <summary>
/// A platform-operator login (the SaaS vendor's own staff), entirely separate from any tenant's
/// per-tenant "Admin" User/Role - manages the Tenant list itself (see PlatformTenantsController),
/// not any one company's business data. Inherits AuditableEntity directly, not TenantOwnedEntity,
/// since it isn't scoped to any tenant at all.
/// </summary>
public class PlatformAdmin : AuditableEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
