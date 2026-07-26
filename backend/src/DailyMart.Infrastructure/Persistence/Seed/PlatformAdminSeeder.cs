using DailyMart.Domain.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// Mirrors AdminSeeder, for the platform-operator login instead of a tenant's first user. No
/// IgnoreQueryFilters needed here (unlike AdminSeeder's Users check) - PlatformAdmin isn't a
/// TenantOwnedEntity at all, so it was never subject to the tenant filter to begin with. Runs once at
/// startup; a no-op once any PlatformAdmin row exists.
/// </summary>
public class PlatformAdminSeeder
{
    private readonly DailyMartDbContext _context;
    private readonly IPasswordHasher<PlatformAdmin> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlatformAdminSeeder> _logger;

    public PlatformAdminSeeder(
        DailyMartDbContext context,
        IPasswordHasher<PlatformAdmin> passwordHasher,
        IConfiguration configuration,
        ILogger<PlatformAdminSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.PlatformAdmins.AnyAsync(cancellationToken))
        {
            return;
        }

        var username = _configuration["PlatformAdmin:DefaultUsername"];
        var password = _configuration["PlatformAdmin:DefaultPassword"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "No platform admins exist and PlatformAdmin:DefaultUsername/DefaultPassword are not " +
                "configured - skipping platform admin seed. The platform panel will not be reachable " +
                "until a PlatformAdmin row is created.");
            return;
        }

        var platformAdmin = new PlatformAdmin
        {
            Username = username,
            FullName = "Platform Administrator",
            IsActive = true
        };
        platformAdmin.PasswordHash = _passwordHasher.HashPassword(platformAdmin, password);

        _context.PlatformAdmins.Add(platformAdmin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default platform admin '{Username}'.", username);
    }
}
