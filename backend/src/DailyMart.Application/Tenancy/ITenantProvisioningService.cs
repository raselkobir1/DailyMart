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
    /// menu, a ShopSettings row (ShopEmail populated from shopEmail - required at signup, see
    /// RegisterRequestValidator - unlike its usual optional/blank default for an existing tenant that
    /// hasn't visited Settings yet), and the first User (adminPasswordHash is already hashed - this
    /// service has no opinion on hashing). One atomic unit - if any step fails, nothing is
    /// committed.</summary>
    Task<User> ProvisionNewTenantAsync(
        string companyName,
        string adminUsername,
        string adminPasswordHash,
        string adminFullName,
        string shopEmail,
        CancellationToken cancellationToken = default);

    /// <summary>Ensures the given (already-existing) tenant has an "Admin" role with CanView/Create/
    /// Edit/Delete=true on every menu currently *available* to it - every Menu.IsGenerallyAvailable=true
    /// row, plus any restricted menu it holds an active TenantFeatureGrant for - creating the role if
    /// missing, upgrading any partial grants to full, and adding grants for any available menu the
    /// tenant doesn't have yet. This is RbacSeeder's old per-boot logic, now scoped to one tenant and
    /// callable on demand: once at signup for a brand-new tenant (via ProvisionNewTenantAsync), once per
    /// existing tenant at every boot (via RbacSeeder) so a newly added generally-available menu still
    /// reaches everyone automatically, and once whenever IFeatureEntitlementService grants a tenant a
    /// restricted menu, so that takes effect immediately rather than waiting for the next boot.
    /// Deliberately never removes access here - see RevokeMenuAccessAsync for that.</summary>
    Task EnsureAdminRoleHasFullMenuAccessAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>Strips every role's (not just Admin's) RoleMenuPermission for one specific menu, for one
    /// specific tenant - called by IFeatureEntitlementService.RevokeAsync right after it soft-deletes the
    /// tenant's TenantFeatureGrant, so a revoked restricted menu actually disappears for every role a
    /// tenant's own Admin may have separately given it to (e.g. a custom "Manager" role), not just the
    /// Admin role this class otherwise only ever adds to.</summary>
    Task RevokeMenuAccessAsync(long tenantId, long menuId, CancellationToken cancellationToken = default);
}
