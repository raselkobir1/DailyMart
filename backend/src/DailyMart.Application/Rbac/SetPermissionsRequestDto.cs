namespace DailyMart.Application.Rbac;

/// <summary>The whole permission matrix for one role, replacing it in one call - RoleId comes from the
/// route (nested under /api/roles/{roleId}/permissions), not this body.</summary>
public class SetPermissionsRequestDto
{
    public List<MenuPermissionItemDto> Permissions { get; init; } = [];
}
