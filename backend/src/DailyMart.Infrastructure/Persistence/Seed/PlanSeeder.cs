using DailyMart.Domain.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Persistence.Seed;

/// <summary>
/// Ensures exactly one seeded "Free" Plan exists, then backfills a TenantSubscription (on that Free
/// plan) for every existing Tenant that doesn't have one yet - covering both the seeded "Default
/// Company" tenant (created directly by the AddTenantsAndPlatformAdmins migration, so it never goes
/// through ITenantProvisioningService.ProvisionNewTenantAsync) and any tenant that predates this
/// feature entirely. Going forward, ProvisionNewTenantAsync creates the subscription itself at signup
/// time, so this backfill loop is normally a no-op after the first boot post-upgrade - same shape as
/// RbacSeeder's per-tenant loop. Runs once at startup; order-independent relative to the other seeders,
/// it only needs migrations to have already run.
/// </summary>
public class PlanSeeder
{
    private const string FreePlanName = "Free";

    private readonly DailyMartDbContext _context;
    private readonly ILogger<PlanSeeder> _logger;

    public PlanSeeder(DailyMartDbContext context, ILogger<PlanSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var freePlan = await _context.Plans.FirstOrDefaultAsync(p => p.IsFree, cancellationToken);

        if (freePlan is null)
        {
            freePlan = new Plan
            {
                Name = FreePlanName,
                Description = "Get started at no cost.",
                Price = 0m,
                BillingCycle = BillingCycle.Monthly,
                IsFree = true,
                IsActive = true,
                SortOrder = 0
            };
            _context.Plans.Add(freePlan);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded default '{PlanName}' plan.", FreePlanName);
        }

        var tenantIdsWithSubscription = await _context.TenantSubscriptions
            .Select(ts => ts.TenantId)
            .ToListAsync(cancellationToken);

        var tenantsWithoutSubscription = await _context.Tenants
            .Where(t => !tenantIdsWithSubscription.Contains(t.Id))
            .ToListAsync(cancellationToken);

        if (tenantsWithoutSubscription.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var tenant in tenantsWithoutSubscription)
        {
            _context.TenantSubscriptions.Add(new TenantSubscription
            {
                TenantId = tenant.Id,
                PlanId = freePlan.Id,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = null
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Backfilled {Count} tenant(s) onto the '{PlanName}' plan.", tenantsWithoutSubscription.Count, FreePlanName);
    }
}
