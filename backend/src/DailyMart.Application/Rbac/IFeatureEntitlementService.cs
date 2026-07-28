namespace DailyMart.Application.Rbac;

/// <summary>
/// The per-tenant "does this tenant even have this menu" gate that sits in front of the existing
/// per-role CanView/CanCreate/CanEdit/CanDelete permission model - see Menu.IsGenerallyAvailable's doc
/// comment. Every generally-available menu needs no entry point here at all; this only matters for a
/// menu a developer has marked exclusive/beta in RbacSeeder's seed list, which a tenant then only gets
/// via an explicit grant from the platform-admin panel.
/// </summary>
public interface IFeatureEntitlementService
{
    /// <summary>The set of menu ids available to a tenant right now: every generally-available menu plus
    /// any restricted menu it holds an active grant for. The one place RoleService.GetPermissionsAsync/
    /// SetPermissionsAsync and AuthService.GetMyPermissionsAsync should read from, so a tenant's own
    /// Admin can neither see nor self-grant access to a menu the platform hasn't entitled it to.</summary>
    Task<HashSet<long>> GetAvailableMenuIdsAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>Whether a tenant can use the menu identified by its stable Key (the same identifier the
    /// frontend's route guards use - see Menu's doc comment). Backs RequireFeatureAttribute, the
    /// backend-enforced gate for a controller/action tied to a restricted menu. Returns false for an
    /// unknown key rather than throwing - an unrecognized key should never accidentally grant access.</summary>
    Task<bool> IsMenuAvailableAsync(long tenantId, string menuKey, CancellationToken cancellationToken = default);

    /// <summary>Every current Menu, denormalized with this tenant's availability - powers the
    /// platform-admin "features" screen.</summary>
    Task<IReadOnlyList<TenantMenuAvailabilityDto>> GetMenuAvailabilityForTenantAsync(
        long tenantId, CancellationToken cancellationToken = default);

    /// <summary>Grants a tenant explicit access to a restricted menu and immediately re-syncs its Admin
    /// role's permissions, so the menu shows up without waiting for the next boot. Idempotent - granting
    /// an already-granted menu is a no-op. Throws BusinessRuleException if the menu is generally
    /// available already (nothing to grant).</summary>
    Task GrantAsync(long tenantId, long menuId, CancellationToken cancellationToken = default);

    /// <summary>Revokes a tenant's explicit grant and strips every role's (not just Admin's)
    /// RoleMenuPermission for that menu, so it actually disappears. No-op if the tenant has no active
    /// grant for it. Throws BusinessRuleException if the menu is generally available (can't be revoked
    /// per-tenant).</summary>
    Task RevokeAsync(long tenantId, long menuId, CancellationToken cancellationToken = default);
}
