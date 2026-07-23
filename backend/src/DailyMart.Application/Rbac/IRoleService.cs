using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Rbac;

public interface IRoleService
{
    Task<PagedResult<RoleDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<RoleDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(RoleRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Throws BusinessRuleException for a system role - Name/Description on "Admin" can't
    /// change, so there's always a role nobody can accidentally rename into uselessness.</summary>
    Task<RoleDto> UpdateAsync(long id, RoleRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Throws BusinessRuleException for a system role, or if any User currently has this role
    /// (checked by name, since User.Role is a plain string - see Role's doc comment) - deleting a role
    /// out from under an active user would leave them with a dangling, unmatched role name.</summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>Every menu, in sidebar order, with this role's current CRUD flags (all false for a menu
    /// this role has no permission row for yet) - the permission-matrix screen's data source.</summary>
    Task<IReadOnlyList<MenuPermissionDto>> GetPermissionsAsync(long roleId, CancellationToken cancellationToken = default);

    /// <summary>Replaces this role's entire permission matrix in one call - updates existing
    /// (role, menu) rows in place and creates any missing ones, rather than delete-then-recreate (keeps
    /// the audit trail to one Updated/Created entry per row that actually changed).</summary>
    Task SetPermissionsAsync(
        long roleId, SetPermissionsRequestDto request, CancellationToken cancellationToken = default);
}
