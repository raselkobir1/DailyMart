using DailyMart.Domain.Auth;
using DailyMart.Domain.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// Self-service registration (POST /api/auth/register) is how every OTHER tenant gets created, but a
/// brand new deployment still needs one seeded "Default Company" tenant + admin user out of the box -
/// see docker-compose.yml's comment on why nothing here can assume a manual setup step. Runs once at
/// startup, AFTER RbacSeeder (which already guarantees the "Default Company" tenant - created by the
/// AddTenantsAndPlatformAdmins migration - has a fully-granted "Admin" role by the time this runs, the
/// same as it does for every other existing tenant); a no-op once any user row exists anywhere
/// (checked with IgnoreQueryFilters - see UserRepository.GetByUsernameAsync's doc comment for why the
/// automatic tenant filter can't apply to a check like this one, which by definition runs before any
/// tenant has a user yet).
/// </summary>
public class AdminSeeder
{
    private readonly DailyMartDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        DailyMartDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration,
        ILogger<AdminSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var username = _configuration["Admin:DefaultUsername"];
        var password = _configuration["Admin:DefaultPassword"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "No users exist and Admin:DefaultUsername/Admin:DefaultPassword are not configured - " +
                "skipping admin seed. Login will not be possible until a user registers via POST /api/auth/register.");
            return;
        }

        // The migration that introduced tenants always creates exactly one "Default Company" row (see
        // AddTenantsAndPlatformAdmins), whether this is a genuinely fresh install or one being upgraded
        // from a pre-multi-tenant deployment with existing data - either way, it's the tenant this
        // seeded admin belongs to.
        var defaultTenant = await _context.Tenants.OrderBy(t => t.Id).FirstAsync(cancellationToken);

        if (!await _context.ShopSettings.IgnoreQueryFilters().AnyAsync(s => s.TenantId == defaultTenant.Id, cancellationToken))
        {
            _context.ShopSettings.Add(new ShopSettings { TenantId = defaultTenant.Id, ShopName = defaultTenant.Name });
        }

        var admin = new User
        {
            TenantId = defaultTenant.Id,
            Username = username,
            FullName = "Administrator",
            Role = "Admin",
            IsActive = true
        };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, password);
        _context.Users.Add(admin);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default admin user '{Username}' for tenant '{Tenant}'.", username, defaultTenant.Name);
    }
}
