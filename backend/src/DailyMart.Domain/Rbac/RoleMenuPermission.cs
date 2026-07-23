using DailyMart.Domain.Common;

namespace DailyMart.Domain.Rbac;

/// <summary>
/// One row per (Role, Menu) pair - the actual permission grant. Four independent CRUD flags per menu,
/// not a single "can access" bit and not per-field/per-action permissions beyond these four - matches the
/// granularity of the RBAC model this was ported from. CanView drives whether the menu even appears in the
/// sidebar/route guard; Create/Edit/Delete are read by the frontend to hide the corresponding buttons, but
/// are not independently enforced by the business controllers themselves (Products, Purchases, etc.) -
/// see RbacSeeder's doc comment for why that's a deliberate, not accidental, scope boundary.
/// </summary>
public class RoleMenuPermission : AuditableEntity
{
    public long RoleId { get; set; }

    public long MenuId { get; set; }

    public bool CanView { get; set; }

    public bool CanCreate { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}
