using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Rbac;

namespace DailyMart.Application.Rbac;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeatureEntitlementService _featureEntitlementService;
    private readonly ICurrentTenantService _currentTenantService;

    public RoleService(
        IUnitOfWork unitOfWork,
        IFeatureEntitlementService featureEntitlementService,
        ICurrentTenantService currentTenantService)
    {
        _unitOfWork = unitOfWork;
        _featureEntitlementService = featureEntitlementService;
        _currentTenantService = currentTenantService;
    }

    private IRepository<Role> Repository => _unitOfWork.Repository<Role>();

    public async Task<PagedResult<RoleDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Role, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : role => role.Name.Contains(request.SearchTerm);

        var result = await Repository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<RoleDto>
        {
            Items = result.Items.Select(r => r.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<RoleDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<RoleDto> CreateAsync(RoleRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var role = request.ToEntity();
        await Repository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return role.ToDto();
    }

    public async Task<RoleDto> UpdateAsync(
        long id, RoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var role = await GetEntityAsync(id, cancellationToken);

        if (role.IsSystem)
        {
            throw new BusinessRuleException($"System role '{role.Name}' cannot be renamed or edited.");
        }

        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        var oldName = role.Name;
        request.ApplyTo(role);

        Repository.Update(role);

        // User.Role is a plain string snapshot of the role name (see User's doc comment), not a foreign
        // key - renaming a role would otherwise silently orphan every user assigned to it: their Role
        // column would no longer match any Role row, and GetMyPermissionsAsync's lookup would find
        // nothing and return zero permitted menus, effectively locking them out with no error.
        if (!string.Equals(oldName, role.Name, StringComparison.Ordinal))
        {
            var affectedUsers = await _unitOfWork.Repository<User>().FindAsync(u => u.Role == oldName, cancellationToken);
            foreach (var user in affectedUsers)
            {
                user.Role = role.Name;
                _unitOfWork.Repository<User>().Update(user);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return role.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await GetEntityAsync(id, cancellationToken);

        if (role.IsSystem)
        {
            throw new BusinessRuleException($"System role '{role.Name}' cannot be deleted.");
        }

        var assignedToAnyUser = await _unitOfWork.Repository<User>()
            .ExistsAsync(u => u.Role == role.Name, cancellationToken);
        if (assignedToAnyUser)
        {
            throw new BusinessRuleException(
                $"Role '{role.Name}' is still assigned to one or more users and cannot be deleted.");
        }

        Repository.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuPermissionDto>> GetPermissionsAsync(
        long roleId, CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(roleId, cancellationToken);

        // Only menus this tenant is actually entitled to are offered here - otherwise a tenant's own
        // Admin could self-grant CanView on a restricted menu the platform never entitled them to (see
        // IFeatureEntitlementService's doc comment).
        var availableMenuIds = await _featureEntitlementService.GetAvailableMenuIdsAsync(
            _currentTenantService.TenantId ?? 0, cancellationToken);

        var menus = await _unitOfWork.Repository<Menu>().GetAllAsync(cancellationToken);
        var permissions = await _unitOfWork.Repository<RoleMenuPermission>()
            .FindAsync(p => p.RoleId == roleId, cancellationToken);
        var permissionsByMenu = permissions.ToDictionary(p => p.MenuId);

        return menus
            .Where(m => availableMenuIds.Contains(m.Id))
            .OrderBy(m => m.SortOrder)
            .Select(m =>
            {
                permissionsByMenu.TryGetValue(m.Id, out var permission);
                return new MenuPermissionDto
                {
                    MenuId = m.Id,
                    MenuKey = m.Key,
                    Label = m.Label,
                    Route = m.Route,
                    Icon = m.Icon,
                    SortOrder = m.SortOrder,
                    ParentId = m.ParentId,
                    CanView = permission?.CanView ?? false,
                    CanCreate = permission?.CanCreate ?? false,
                    CanEdit = permission?.CanEdit ?? false,
                    CanDelete = permission?.CanDelete ?? false
                };
            })
            .ToList();
    }

    public async Task SetPermissionsAsync(
        long roleId, SetPermissionsRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureRoleExistsAsync(roleId, cancellationToken);

        var menuIds = request.Permissions.Select(p => p.MenuId).Distinct().ToList();
        var existingMenuCount = await _unitOfWork.Repository<Menu>()
            .FindAsync(m => menuIds.Contains(m.Id), cancellationToken);
        if (existingMenuCount.Count != menuIds.Count)
        {
            throw new BusinessRuleException("One or more menus in the permission list do not exist.");
        }

        // Rejects a submission naming a menu this tenant isn't entitled to - GetPermissionsAsync already
        // never offers one, but that alone only hides it from the UI; without this check a tenant Admin
        // could still self-grant it by posting the id directly.
        var availableMenuIds = await _featureEntitlementService.GetAvailableMenuIdsAsync(
            _currentTenantService.TenantId ?? 0, cancellationToken);
        if (menuIds.Except(availableMenuIds).Any())
        {
            throw new BusinessRuleException("One or more menus in the permission list are not available to this tenant.");
        }

        var permissionRepository = _unitOfWork.Repository<RoleMenuPermission>();
        var existingPermissions = (await permissionRepository.FindAsync(p => p.RoleId == roleId, cancellationToken))
            .ToDictionary(p => p.MenuId);

        foreach (var item in request.Permissions)
        {
            if (existingPermissions.TryGetValue(item.MenuId, out var existing))
            {
                existing.CanView = item.CanView;
                existing.CanCreate = item.CanCreate;
                existing.CanEdit = item.CanEdit;
                existing.CanDelete = item.CanDelete;
                permissionRepository.Update(existing);
            }
            else
            {
                await permissionRepository.AddAsync(new RoleMenuPermission
                {
                    RoleId = roleId,
                    MenuId = item.MenuId,
                    CanView = item.CanView,
                    CanCreate = item.CanCreate,
                    CanEdit = item.CanEdit,
                    CanDelete = item.CanDelete
                }, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Role), id);

    private async Task EnsureRoleExistsAsync(long roleId, CancellationToken cancellationToken)
    {
        if (!await Repository.ExistsAsync(r => r.Id == roleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Role), roleId);
        }
    }

    private async Task EnsureNameIsUniqueAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var duplicateExists = await Repository.ExistsAsync(
            role => role.Name.ToLower() == normalizedName && (excludeId == null || role.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A role named '{name}' already exists.");
        }
    }
}
