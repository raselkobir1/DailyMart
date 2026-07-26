using DailyMart.Domain.Auth;

namespace DailyMart.Application.Tenancy;

/// <summary>
/// Creates and bootstraps tenants (companies/shops) - the operations a normal per-tenant-request
/// service can't do, since they run before any tenant context exists (self-service signup is
/// anonymous) or need to reach across every tenant at once (the boot-time "every tenant's Admin
/// sees every current menu" guarantee). Implemented in Infrastructure, not Application, because it
/// needs to bypass the automatic tenant query filter (IgnoreQueryFilters) the way the existing
/// seeders already do - see TenantProvisioningService's doc comment.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>Creates a brand new Tenant, its own "Admin" role with full access to every current
    /// menu, a default ShopSettings row, and the first User (adminPasswordHash is already hashed -
    /// this service has no opinion on hashing). One atomic unit - if any step fails, nothing is
    /// committed.</summary>
    Task<User> ProvisionNewTenantAsync(
        string companyName,
        string adminUsername,
        string adminPasswordHash,
        string adminFullName,
        CancellationToken cancellationToken = default);

    /// <summary>Ensures the given (already-existing) tenant has an "Admin" role with CanView/Create/
    /// Edit/Delete=true on every current global Menu - creating the role if missing, upgrading any
    /// partial grants to full, and adding grants for any menu the tenant doesn't have yet. This is
    /// RbacSeeder's old per-boot logic, now scoped to one tenant and callable on demand: once at
    /// signup for a brand-new tenant (via ProvisionNewTenantAsync), and once per existing tenant at
    /// every boot (via RbacSeeder) so a newly added menu still reaches everyone automatically.</summary>
    Task EnsureAdminRoleHasFullMenuAccessAsync(long tenantId, CancellationToken cancellationToken = default);
}
