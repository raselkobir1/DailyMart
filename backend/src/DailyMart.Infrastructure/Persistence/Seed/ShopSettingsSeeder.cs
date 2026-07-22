using DailyMart.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// Settings is a singleton (Module 2 §1) - this guarantees the one row exists so the rest of the app
/// never has to handle "no settings yet" as a state. Runs once at startup; a no-op once a row exists.
/// </summary>
public class ShopSettingsSeeder
{
    private readonly DailyMartDbContext _context;
    private readonly ILogger<ShopSettingsSeeder> _logger;

    public ShopSettingsSeeder(DailyMartDbContext context, ILogger<ShopSettingsSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.ShopSettings.AnyAsync(cancellationToken))
        {
            return;
        }

        _context.ShopSettings.Add(new ShopSettings { ShopName = "DailyMart" });
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded default shop settings.");
    }
}
