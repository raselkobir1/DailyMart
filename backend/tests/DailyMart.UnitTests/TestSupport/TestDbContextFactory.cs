using DailyMart.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.UnitTests.TestSupport;

public static class TestDbContextFactory
{
    /// <summary>Creates a fresh, isolated in-memory TestDbContext with the real audit interceptor attached.</summary>
    public static TestDbContext Create(FakeCurrentUserService? currentUserService = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditingSaveChangesInterceptor(currentUserService ?? new FakeCurrentUserService()))
            .Options;

        return new TestDbContext(options);
    }
}
