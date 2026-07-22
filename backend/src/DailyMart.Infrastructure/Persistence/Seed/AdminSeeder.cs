using DailyMart.Domain.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// There's no self-registration (single shop, single admin, per CLAUDE.md §1) - this is the only way to
/// get a first user into the system. Runs once at startup; a no-op once any user row exists.
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
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var username = _configuration["Admin:DefaultUsername"];
        var password = _configuration["Admin:DefaultPassword"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning(
                "No users exist and Admin:DefaultUsername/Admin:DefaultPassword are not configured - " +
                "skipping admin seed. Login will not be possible until a user is created.");
            return;
        }

        var admin = new User
        {
            Username = username,
            FullName = "Administrator",
            Role = "Admin",
            IsActive = true
        };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, password);

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default admin user '{Username}'.", username);
    }
}
