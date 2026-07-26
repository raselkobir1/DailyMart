using DailyMart.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.UnitTests.TestSupport;

public static class TestDbContextFactory
{
    /// <summary>Creates a fresh, isolated in-memory TestDbContext with the real audit interceptor
    /// attached. tenantId drives both the interceptor's stamping (via a FakeCurrentTenantService) and
    /// the context's own CurrentTenantId (read by the query filter) together, so they can never drift
    /// apart the way two independent parameters could. Pass the same databaseName across multiple
    /// Create() calls to simulate several different requests (each with its own tenantId) hitting the
    /// same physical database - the shape a real tenant-isolation test needs.</summary>
    public static TestDbContext Create(
        FakeCurrentUserService? currentUserService = null, long? tenantId = null, string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .AddInterceptors(new AuditingSaveChangesInterceptor(
                currentUserService ?? new FakeCurrentUserService(),
                new FakeCurrentTenantService { TenantId = tenantId }))
            .Options;

        return new TestDbContext(options) { CurrentTenantId = tenantId };
    }
}
