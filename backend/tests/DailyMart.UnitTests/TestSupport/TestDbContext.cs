using DailyMart.Domain.Auditing;
using DailyMart.Domain.Tenancy;
using DailyMart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.UnitTests.TestSupport;

/// <summary>
/// Mirrors DailyMartDbContext's model-building conventions (soft-delete + tenant-isolation filters)
/// against TestWidget/TestTenantWidget instead of real module entities, so the shared convention
/// itself is what's under test - not a hand-rolled copy of it. TestWidget (plain AuditableEntity)
/// only ever gets the soft-delete half, identical to before this file added tenant awareness -
/// TestTenantWidget (TenantOwnedEntity) gets both, exercising exactly what DailyMartDbContext does.
/// </summary>
public class TestDbContext : DbContext, ITenantScopedDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    /// <summary>Settable directly (unlike the real DbContext, which reads this from
    /// ICurrentTenantService/HttpContext) so a test can simulate "this instance is handling a
    /// request for tenant X" without standing up a fake HTTP pipeline.</summary>
    public long? CurrentTenantId { get; set; }

    public DbSet<TestWidget> Widgets => Set<TestWidget>();

    public DbSet<TestTenantWidget> TenantWidgets => Set<TestTenantWidget>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyTenantForeignKeys();
        modelBuilder.ApplyTenancyQueryFilters(this);
        base.OnModelCreating(modelBuilder);
    }
}
