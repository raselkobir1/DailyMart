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

    public RoleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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

        request.ApplyTo(role);

        Repository.Update(role);
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

        var menus = await _unitOfWork.Repository<Menu>().GetAllAsync(cancellationToken);
        var permissions = await _unitOfWork.Repository<RoleMenuPermission>()
            .FindAsync(p => p.RoleId == roleId, cancellationToken);
        var permissionsByMenu = permissions.ToDictionary(p => p.MenuId);

        return menus
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
