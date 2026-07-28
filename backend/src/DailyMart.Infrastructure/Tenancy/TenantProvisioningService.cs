using DailyMart.Application.Tenancy;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Billing;
using DailyMart.Domain.Rbac;
using DailyMart.Domain.Settings;
using DailyMart.Domain.Tenancy;
using DailyMart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Tenancy;

/// <summary>
/// Lives in Infrastructure, not Application, and talks to DailyMartDbContext directly rather than
/// going through IUnitOfWork/IRepository&lt;T&gt; - same reason the existing seeders (AdminSeeder,
/// RbacSeeder) do the same thing: every read here needs IgnoreQueryFilters() (there is no tenant
/// context yet for a brand-new tenant, or for a boot-time loop over every tenant), and every write
/// stamps TenantId explicitly rather than relying on AuditingSaveChangesInterceptor's auto-stamp
/// (which only fires when there IS a current tenant - see its own doc comment).
/// </summary>
public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly DailyMartDbContext _context;

    public TenantProvisioningService(DailyMartDbContext context)
    {
        _context = context;
    }

    public async Task<User> ProvisionNewTenantAsync(
        string companyName,
        string adminUsername,
        string adminPasswordHash,
        string adminFullName,
        string shopEmail,
        CancellationToken cancellationToken = default)
    {
        var tenant = new Tenant { Name = companyName, IsActive = true };
        _context.Tenants.Add(tenant);
        // Saved now so tenant.Id is populated before everything below references it.
        await _context.SaveChangesAsync(cancellationToken);

        _context.ShopSettings.Add(new ShopSettings { TenantId = tenant.Id, ShopName = companyName, ShopEmail = shopEmail });

        // Every new tenant starts on the Free plan (see PlanSeeder, which guarantees this row exists
        // before any HTTP request - including this anonymous signup one - is ever served).
        var freePlan = await _context.Plans.FirstOrDefaultAsync(p => p.IsFree, cancellationToken)
            ?? throw new InvalidOperationException("No Free plan is seeded - PlanSeeder should have created one at startup.");

        _context.TenantSubscriptions.Add(new TenantSubscription
        {
            TenantId = tenant.Id,
            PlanId = freePlan.Id,
            CurrentPeriodStart = DateTimeOffset.UtcNow,
            CurrentPeriodEnd = null
        });

        var admin = new User
        {
            TenantId = tenant.Id,
            Username = adminUsername,
            PasswordHash = adminPasswordHash,
            FullName = adminFullName,
            Role = "Admin",
            IsActive = true
        };
        _context.Users.Add(admin);

        await _context.SaveChangesAsync(cancellationToken);

        await EnsureAdminRoleHasFullMenuAccessAsync(tenant.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return admin;
    }

    public async Task EnsureAdminRoleHasFullMenuAccessAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var adminRole = await _context.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == "Admin" && !r.IsDeleted, cancellationToken);

        if (adminRole is null)
        {
            adminRole = new Role
            {
                TenantId = tenantId,
                Name = "Admin",
                Description = "Full access to every menu - cannot be renamed or deleted.",
                IsSystem = true,
                IsDefault = false
            };
            _context.Roles.Add(adminRole);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var menuIds = await GetAvailableMenuIdsAsync(tenantId, cancellationToken);

        var existingPermissions = await _context.RoleMenuPermissions.IgnoreQueryFilters()
            .Where(p => p.RoleId == adminRole.Id && !p.IsDeleted)
            .ToDictionaryAsync(p => p.MenuId, cancellationToken);

        foreach (var menuId in menuIds)
        {
            if (existingPermissions.TryGetValue(menuId, out var permission))
            {
                if (permission.CanView && permission.CanCreate && permission.CanEdit && permission.CanDelete)
                {
                    continue;
                }

                permission.CanView = true;
                permission.CanCreate = true;
                permission.CanEdit = true;
                permission.CanDelete = true;
                continue;
            }

            _context.RoleMenuPermissions.Add(new RoleMenuPermission
            {
                TenantId = tenantId,
                RoleId = adminRole.Id,
                MenuId = menuId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true
            });
        }
    }

    public async Task RevokeMenuAccessAsync(long tenantId, long menuId, CancellationToken cancellationToken = default)
    {
        var permissions = await _context.RoleMenuPermissions.IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && p.MenuId == menuId && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var permission in permissions)
        {
            _context.RoleMenuPermissions.Remove(permission);
        }
    }

    /// <summary>Every Menu.IsGenerallyAvailable=true id, plus any restricted menu this tenant holds an
    /// active TenantFeatureGrant for. Queried directly against _context (not through
    /// IFeatureEntitlementService) so this class stays self-contained the way the rest of it already is -
    /// injecting the Application-layer grant/revoke service here would create a circular dependency,
    /// since that service in turn calls back into this one to re-sync Admin's permissions after a grant.
    /// TenantFeatureGrant is AuditableEntity-direct like Menu (global, not tenant-filtered), so its
    /// model-level query filter is soft-delete-only - no IgnoreQueryFilters() needed here, same as every
    /// other Menu/TenantSubscription-style query in this class.</summary>
    private async Task<List<long>> GetAvailableMenuIdsAsync(long tenantId, CancellationToken cancellationToken)
    {
        var generallyAvailableMenuIds = await _context.Menus
            .Where(m => m.IsGenerallyAvailable)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        var grantedMenuIds = await _context.TenantFeatureGrants
            .Where(g => g.TenantId == tenantId)
            .Select(g => g.MenuId)
            .ToListAsync(cancellationToken);

        return generallyAvailableMenuIds.Union(grantedMenuIds).ToList();
    }
}
