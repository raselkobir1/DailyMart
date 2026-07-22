using DailyMart.Domain.Auditing;
using DailyMart.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.UnitTests.TestSupport;

/// <summary>
/// Mirrors DailyMartDbContext's model-building conventions (soft-delete filter) against TestWidget
/// instead of a real module entity, so the shared convention itself is what's under test - not a
/// hand-rolled copy of it.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestWidget> Widgets => Set<TestWidget>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySoftDeleteQueryFilter();
        base.OnModelCreating(modelBuilder);
    }
}
