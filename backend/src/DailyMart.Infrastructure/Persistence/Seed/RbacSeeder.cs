using DailyMart.Domain.Rbac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// Runs on every startup (not gated by "if any Role exists, skip" the way AdminSeeder is) and upserts
/// idempotently, menu by menu - so adding a new module's Menu row to <see cref="SeedMenus"/> and
/// redeploying is enough to make it show up for Admin, with no manual "go grant permissions" step. This
/// mirrors the RBAC model this was ported from: "grants full CRUD on any newly created menu... nothing
/// needs manual re-granting."
///
/// Deliberately only grants Admin access here - no other role is seeded, since a "Cashier"/"Manager" role
/// with a deliberately restricted menu set is exactly the kind of thing this system exists so an admin can
/// configure themselves via the Roles/Permissions screens, not something to hardcode.
/// </summary>
public class RbacSeeder
{
    private readonly DailyMartDbContext _context;
    private readonly ILogger<RbacSeeder> _logger;

    /// <summary>The menu/screen set DailyMart actually has today - keep in sync with app.routes.ts.
    /// SortOrder mirrors the intended sidebar order.</summary>
    private static readonly (string Key, string Label, string Route, string Icon, int SortOrder)[] SeedMenus =
    [
        ("dashboard", "Dashboard", "/dashboard", "📊", 5),
        ("products", "Products", "/products", "🛍️", 10),
        ("categories", "Categories", "/categories", "🏷️", 20),
        ("brands", "Brands", "/brands", "🔖", 30),
        ("units", "Units", "/units", "📏", 40),
        ("suppliers", "Suppliers", "/suppliers", "🚚", 50),
        ("customers", "Customers", "/customers", "🧑‍🤝‍🧑", 60),
        ("purchases", "Purchases", "/purchases", "🧾", 70),
        ("inventory", "Inventory", "/inventory", "📦", 80),
        ("pos", "POS", "/pos", "🖥️", 90),
        ("sales", "Sales", "/sales", "💰", 100),
        ("expenses", "Expenses", "/expenses", "🧮", 105),
        ("profit-loss", "Profit & Loss", "/profit-loss", "📈", 106),
        ("reports", "Reports", "/reports", "📑", 107),
        ("audit-log", "Audit Log", "/audit-log", "📜", 110),
        ("settings", "Settings", "/settings", "⚙️", 120),
        ("users", "Users", "/users", "👥", 130),
        ("roles", "Roles", "/roles", "🛡️", 140),
        ("menus", "Menus", "/menus", "🧭", 150),
        ("permissions", "Permissions", "/permissions", "🔐", 160)
    ];

    public RbacSeeder(DailyMartDbContext context, ILogger<RbacSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminRole = await GetOrCreateAdminRoleAsync(cancellationToken);
        var menuIds = await GetOrCreateMenusAsync(cancellationToken);
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

    private async Task<List<long>> GetOrCreateMenusAsync(CancellationToken cancellationToken)
    {
        var existingByKey = await _context.Menus.ToDictionaryAsync(m => m.Key, cancellationToken);
        var menuIds = new List<long>();

        foreach (var seed in SeedMenus)
        {
            if (existingByKey.TryGetValue(seed.Key, out var existing))
            {
                menuIds.Add(existing.Id);
                continue;
            }

            var menu = new Menu
            {
                Key = seed.Key,
                Label = seed.Label,
                Route = seed.Route,
                Icon = seed.Icon,
                SortOrder = seed.SortOrder
            };
            _context.Menus.Add(menu);
            await _context.SaveChangesAsync(cancellationToken);

            menuIds.Add(menu.Id);
            _logger.LogInformation("Seeded menu '{Key}'.", seed.Key);
        }

        return menuIds;
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
}
