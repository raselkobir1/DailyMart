using DailyMart.Domain.Common;

namespace DailyMart.Domain.Rbac;

/// <summary>
/// One row per sidebar/navigable screen. Key is the stable identifier the frontend's route guards and
/// Perms service match against (e.g. "products") - Route/Label/Icon/SortOrder can change freely, Key can't
/// (see MenuService.UpdateAsync, which has no Key parameter at all). ParentId supports unlimited-depth
/// nesting for future sub-menus, though the seeded set is flat today - same "supported but unused"
/// stance as the RBAC model this was ported from.
/// </summary>
public class Menu : AuditableEntity
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public long? ParentId { get; set; }
}
