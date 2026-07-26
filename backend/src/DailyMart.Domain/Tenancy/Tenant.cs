using DailyMart.Domain.Common;

namespace DailyMart.Domain.Tenancy;

/// <summary>
/// One row per company/shop that signed up. Deliberately global/unscoped - a tenant obviously
/// doesn't belong to itself - so it inherits AuditableEntity directly, not TenantOwnedEntity, and
/// is excluded from the tenant query filter the same way Menu/PlatformAdmin are.
/// IsActive gates login (see AuthService) - suspending a tenant here blocks every one of its
/// users from logging in or refreshing a token, without deleting any of their data.
/// </summary>
public class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
