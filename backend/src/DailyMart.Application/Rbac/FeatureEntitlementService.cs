using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Tenancy;
using DailyMart.Domain.Rbac;

namespace DailyMart.Application.Rbac;

public class FeatureEntitlementService : IFeatureEntitlementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvisioningService _tenantProvisioningService;

    public FeatureEntitlementService(IUnitOfWork unitOfWork, ITenantProvisioningService tenantProvisioningService)
    {
        _unitOfWork = unitOfWork;
        _tenantProvisioningService = tenantProvisioningService;
    }

    private IRepository<Menu> Menus => _unitOfWork.Repository<Menu>();

    private IRepository<TenantFeatureGrant> Grants => _unitOfWork.Repository<TenantFeatureGrant>();

    public async Task<HashSet<long>> GetAvailableMenuIdsAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var menus = await Menus.GetAllAsync(cancellationToken);
        var grants = await Grants.FindAsync(g => g.TenantId == tenantId, cancellationToken);

        var availableMenuIds = menus.Where(m => m.IsGenerallyAvailable).Select(m => m.Id).ToHashSet();
        foreach (var grant in grants)
        {
            availableMenuIds.Add(grant.MenuId);
        }

        return availableMenuIds;
    }

    public async Task<bool> IsMenuAvailableAsync(long tenantId, string menuKey, CancellationToken cancellationToken = default)
    {
        var menu = (await Menus.FindAsync(m => m.Key == menuKey, cancellationToken)).FirstOrDefault();
        if (menu is null)
        {
            return false;
        }

        if (menu.IsGenerallyAvailable)
        {
            return true;
        }

        return await Grants.ExistsAsync(g => g.TenantId == tenantId && g.MenuId == menu.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMenuAvailabilityDto>> GetMenuAvailabilityForTenantAsync(
        long tenantId, CancellationToken cancellationToken = default)
    {
        var menus = await Menus.GetAllAsync(cancellationToken);
        var grantedMenuIds = (await Grants.FindAsync(g => g.TenantId == tenantId, cancellationToken))
            .Select(g => g.MenuId)
            .ToHashSet();

        return menus
            .OrderBy(m => m.SortOrder)
            .Select(m =>
            {
                var isGranted = grantedMenuIds.Contains(m.Id);
                return new TenantMenuAvailabilityDto
                {
                    MenuId = m.Id,
                    MenuKey = m.Key,
                    Label = m.Label,
                    ParentId = m.ParentId,
                    SortOrder = m.SortOrder,
                    IsGenerallyAvailable = m.IsGenerallyAvailable,
                    IsGranted = isGranted,
                    IsAvailable = m.IsGenerallyAvailable || isGranted
                };
            })
            .ToList();
    }

    public async Task GrantAsync(long tenantId, long menuId, CancellationToken cancellationToken = default)
    {
        var menu = await GetMenuOrThrowAsync(menuId, cancellationToken);
        if (menu.IsGenerallyAvailable)
        {
            throw new BusinessRuleException($"'{menu.Label}' is already available to every tenant - there's nothing to grant.");
        }

        var existingGrant = (await Grants.FindAsync(g => g.TenantId == tenantId && g.MenuId == menuId, cancellationToken))
            .FirstOrDefault();
        if (existingGrant is null)
        {
            await Grants.AddAsync(new TenantFeatureGrant { TenantId = tenantId, MenuId = menuId }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Wires the newly-available menu into the tenant's Admin role immediately, rather than leaving it
        // invisible until the next boot's RbacSeeder pass.
        await _tenantProvisioningService.EnsureAdminRoleHasFullMenuAccessAsync(tenantId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAsync(long tenantId, long menuId, CancellationToken cancellationToken = default)
    {
        var menu = await GetMenuOrThrowAsync(menuId, cancellationToken);
        if (menu.IsGenerallyAvailable)
        {
            throw new BusinessRuleException($"'{menu.Label}' is available to every tenant and can't be revoked per-tenant.");
        }

        var existingGrant = (await Grants.FindAsync(g => g.TenantId == tenantId && g.MenuId == menuId, cancellationToken))
            .FirstOrDefault();
        if (existingGrant is null)
        {
            return;
        }

        Grants.Remove(existingGrant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Strips every role's access to the menu (not just Admin's) so it actually disappears.
        await _tenantProvisioningService.RevokeMenuAccessAsync(tenantId, menuId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Menu> GetMenuOrThrowAsync(long menuId, CancellationToken cancellationToken) =>
        await Menus.GetByIdAsync(menuId, cancellationToken) ?? throw new NotFoundException(nameof(Menu), menuId);
}
