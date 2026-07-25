using DailyMart.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// Runs on every startup (not gated by "if any Role exists, skip" the way AdminSeeder is) and upserts
/// idempotently, menu by menu - so adding a new module's Menu row to <see cref="SeedMenus"/>, or
/// re-parenting/relabeling an existing one, and redeploying is enough to make it show up correctly for
/// Admin, with no manual "go grant permissions" or "go edit the Menu row" step. This mirrors the RBAC
/// model this was ported from: "grants full CRUD on any newly created menu... nothing needs manual
/// re-granting."
///
/// Deliberately only grants Admin access here - no other role is seeded, since a "Cashier"/"Manager" role
/// with a deliberately restricted menu set is exactly the kind of thing this system exists so an admin can
/// configure themselves via the Roles/Permissions screens, not something to hardcode. One consequence
/// worth calling out: introducing a new *parent* group menu (see below) means any existing custom role
/// that could already view a child menu will stop seeing it in the sidebar until that role is also
/// granted CanView on the new parent - GetMyPermissionsAsync filters to CanView=true rows, and the
/// frontend's navTree only descends into a parent's children if the parent itself made it through that
/// filter. Same "manual re-grant for custom roles" policy as any other new menu, just easy to miss here.
/// </summary>
public class RbacSeeder
{
    private readonly DailyMartDbContext _context;
    private readonly ILogger<RbacSeeder> _logger;

    /// <summary>
    /// The menu/screen set DailyMart actually has today - keep in sync with app.routes.ts. Parent group
    /// rows (ParentKey null, themselves referenced by a child's ParentKey) are pure sidebar groupings, not
    /// separate pages - each one's Route points at its first child so clicking the group header still
    /// navigates somewhere real rather than needing a dedicated "no route" concept. SortOrder only needs
    /// to be unique among siblings (children are grouped by parent before sorting), not globally.
    /// </summary>
    private static readonly MenuSeed[] SeedMenus =
    [
        new("dashboard", "Dashboard", "/dashboard", "📊", 10, null),

        new("catalog", "Catalog", "/products", "🗂️", 20, null),
        new("products", "Products", "/products", "🛍️", 10, "catalog"),
        new("categories", "Categories", "/categories", "🏷️", 20, "catalog"),
        new("brands", "Brands", "/brands", "🔖", 30, "catalog"),
        new("units", "Units", "/units", "📏", 40, "catalog"),

        new("partners", "Partners", "/suppliers", "🤝", 30, null),
        new("suppliers", "Suppliers", "/suppliers", "🚚", 10, "partners"),
        new("customers", "Customers", "/customers", "🧑‍🤝‍🧑", 20, "partners"),

        new("purchasing", "Purchasing", "/purchases", "🛒", 40, null),
        new("purchases", "Purchases", "/purchases", "🧾", 10, "purchasing"),
        new("inventory", "Inventory", "/inventory", "📦", 20, "purchasing"),

        new("sales-group", "Sales & POS", "/pos", "🏬", 50, null),
        new("pos", "POS", "/pos", "🖥️", 10, "sales-group"),
        new("sales", "Sales", "/sales", "💰", 20, "sales-group"),

        new("finance", "Finance", "/expenses", "💵", 60, null),
        new("expenses", "Expenses", "/expenses", "🧮", 10, "finance"),
        new("profit-loss", "Profit & Loss", "/profit-loss", "📈", 20, "finance"),

        new("reports", "Reports", "/reports", "📑", 70, null),
        new("settings", "Settings", "/settings", "⚙️", 80, null),

        new("security", "Security", "/users", "🔐", 90, null),
        new("users", "Users", "/users", "👥", 10, "security"),
        new("roles", "Roles", "/roles", "🛡️", 20, "security"),
        new("menus", "Menus", "/menus", "🧭", 30, "security"),
        new("permissions", "Permissions", "/permissions", "🗝️", 40, "security"),
        new("audit-log", "Audit Log", "/audit-log", "📜", 50, "security")
    ];

    public RbacSeeder(DailyMartDbContext context, ILogger<RbacSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminRole = await GetOrCreateAdminRoleAsync(cancellationToken);
        var menuIds = await UpsertMenusAsync(cancellationToken);
        await EnsureAdminHasFullAccessAsync(adminRole.Id, menuIds, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Role> GetOrCreateAdminRoleAsync(CancellationToken cancellationToken)
    {
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
        if (adminRole is not null)
        {
            return adminRole;
        }

        adminRole = new Role
        {
            Name = "Admin",
            Description = "Full access to every menu - cannot be renamed or deleted.",
            IsSystem = true,
            IsDefault = false
        };
        _context.Roles.Add(adminRole);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded system role 'Admin'.");
        return adminRole;
    }

    /// <summary>Two passes so a child's ParentKey can resolve to its parent's DB-generated Id: parent
    /// rows (ParentKey null) are upserted first, then children look up their already-upserted parent.
    /// Updates every field on rows that already exist (not just inserting missing ones) - that's what
    /// lets re-parenting/relabeling an existing menu here actually take effect on redeploy.</summary>
    private async Task<List<long>> UpsertMenusAsync(CancellationToken cancellationToken)
    {
        var existingByKey = await _context.Menus.ToDictionaryAsync(m => m.Key, cancellationToken);
        var idByKey = new Dictionary<string, long>();
        var menuIds = new List<long>();

        foreach (var seed in SeedMenus.Where(s => s.ParentKey is null))
        {
            idByKey[seed.Key] = await UpsertOneAsync(seed, parentId: null, existingByKey, menuIds, cancellationToken);
        }

        foreach (var seed in SeedMenus.Where(s => s.ParentKey is not null))
        {
            var parentId = idByKey[seed.ParentKey!];
            idByKey[seed.Key] = await UpsertOneAsync(seed, parentId, existingByKey, menuIds, cancellationToken);
        }

        return menuIds;
    }

    private async Task<long> UpsertOneAsync(
        MenuSeed seed,
        long? parentId,
        IReadOnlyDictionary<string, Menu> existingByKey,
        List<long> menuIds,
        CancellationToken cancellationToken)
    {
        if (existingByKey.TryGetValue(seed.Key, out var existing))
        {
            if (existing.Label != seed.Label || existing.Route != seed.Route || existing.Icon != seed.Icon ||
                existing.SortOrder != seed.SortOrder || existing.ParentId != parentId)
            {
                existing.Label = seed.Label;
                existing.Route = seed.Route;
                existing.Icon = seed.Icon;
                existing.SortOrder = seed.SortOrder;
                existing.ParentId = parentId;
                _logger.LogInformation("Updated menu '{Key}' to match the current seed definition.", seed.Key);
            }

            menuIds.Add(existing.Id);
            return existing.Id;
        }

        var menu = new Menu
        {
            Key = seed.Key,
            Label = seed.Label,
            Route = seed.Route,
            Icon = seed.Icon,
            SortOrder = seed.SortOrder,
            ParentId = parentId
        };
        _context.Menus.Add(menu);
        await _context.SaveChangesAsync(cancellationToken);

        menuIds.Add(menu.Id);
        _logger.LogInformation("Seeded menu '{Key}'.", seed.Key);
        return menu.Id;
    }

    private async Task EnsureAdminHasFullAccessAsync(
        long adminRoleId, IReadOnlyCollection<long> menuIds, CancellationToken cancellationToken)
    {
        var existingPermissions = await _context.RoleMenuPermissions
            .Where(p => p.RoleId == adminRoleId)
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
                RoleId = adminRoleId,
                MenuId = menuId,
                CanView = true,
                CanCreate = true,
                CanEdit = true,
                CanDelete = true
            });
        }
    }

    private sealed record MenuSeed(string Key, string Label, string Route, string Icon, int SortOrder, string? ParentKey);
}
