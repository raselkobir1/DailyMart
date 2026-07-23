namespace DailyMart.Application.Rbac;

/// <summary>One menu, denormalized with its label/route/icon/hierarchy info, plus one role's CRUD flags
/// for it. Used both for GET /api/roles/{roleId}/permissions (the permission-matrix screen) and GET
/// /api/auth/me/permissions (the CanView=true subset of these, for the current user - drives the dynamic
/// sidebar and route guards).</summary>
public class MenuPermissionDto
{
    public long MenuId { get; init; }

    public string MenuKey { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public long? ParentId { get; init; }

    public bool CanView { get; init; }

    public bool CanCreate { get; init; }

    public bool CanEdit { get; init; }

    public bool CanDelete { get; init; }
}
