using DailyMart.Domain.Tenancy;
using DailyMart.UnitTests.TestSupport;

namespace DailyMart.UnitTests.Persistence;

/// <summary>
/// Proves the tenant query filter (TenancyModelExtensions, exercised here via TestDbContext exactly
/// as DailyMartDbContext uses it - see TestDbContext's doc comment) actually isolates data between
/// tenants at the EF Core level, not just "looks correct by inspection." The critical thing under
/// test: EF Core caches the model (and the query filter's expression tree) once per DbContext type,
/// so these tests specifically use multiple DbContext INSTANCES sharing one physical database to
/// confirm each instance's own CurrentTenantId is honored per query - not the first instance's,
/// which is the failure mode a naive "capture a constant at model-build time" implementation would
/// have.
/// </summary>
public class TenancyQueryFilterTests
{
    [Fact]
    public async Task A_tenants_query_never_returns_another_tenants_rows()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var seedContext = TestDbContextFactory.Create(tenantId: 1, databaseName: dbName))
        {
            seedContext.Tenants.Add(new Tenant { Id = 1, Name = "Tenant One" });
            seedContext.Tenants.Add(new Tenant { Id = 2, Name = "Tenant Two" });
            await seedContext.SaveChangesAsync();
        }

        await using (var tenantOneContext = TestDbContextFactory.Create(tenantId: 1, databaseName: dbName))
        {
            tenantOneContext.TenantWidgets.Add(new TestTenantWidget { Name = "Tenant One's Widget" });
            await tenantOneContext.SaveChangesAsync();
        }

        await using (var tenantTwoContext = TestDbContextFactory.Create(tenantId: 2, databaseName: dbName))
        {
            tenantTwoContext.TenantWidgets.Add(new TestTenantWidget { Name = "Tenant Two's Widget" });
            await tenantTwoContext.SaveChangesAsync();
        }

        // Fresh instances, same physical database, different CurrentTenantId each - the case that
        // would break if the filter had captured the first-ever context instance instead of
        // re-evaluating per instance.
        await using var readAsTenantOne = TestDbContextFactory.Create(tenantId: 1, databaseName: dbName);
        await using var readAsTenantTwo = TestDbContextFactory.Create(tenantId: 2, databaseName: dbName);

        var tenantOneWidgets = readAsTenantOne.TenantWidgets.ToList();
        var tenantTwoWidgets = readAsTenantTwo.TenantWidgets.ToList();

        Assert.Single(tenantOneWidgets);
        Assert.Equal("Tenant One's Widget", tenantOneWidgets[0].Name);

        Assert.Single(tenantTwoWidgets);
        Assert.Equal("Tenant Two's Widget", tenantTwoWidgets[0].Name);
    }

    [Fact]
    public async Task Saving_a_tenant_owned_entity_stamps_TenantId_from_the_interceptors_current_tenant()
    {
        await using var context = TestDbContextFactory.Create(tenantId: 7);
        context.Tenants.Add(new Tenant { Id = 7, Name = "Tenant Seven" });

        var widget = new TestTenantWidget { Name = "Auto-stamped" };
        context.TenantWidgets.Add(widget);
        await context.SaveChangesAsync();

        Assert.Equal(7, widget.TenantId);
    }

    [Fact]
    public async Task A_null_current_tenant_sees_zero_tenant_owned_rows_fail_closed_not_all_rows()
    {
        var dbName = Guid.NewGuid().ToString();

        await using (var seedContext = TestDbContextFactory.Create(tenantId: 1, databaseName: dbName))
        {
            seedContext.Tenants.Add(new Tenant { Id = 1, Name = "Tenant One" });
            seedContext.TenantWidgets.Add(new TestTenantWidget { Name = "Tenant One's Widget" });
            await seedContext.SaveChangesAsync();
        }

        // Simulates a platform-admin token, which never carries a tenant_id claim.
        await using var noTenantContext = TestDbContextFactory.Create(tenantId: null, databaseName: dbName);

        Assert.Empty(noTenantContext.TenantWidgets.ToList());
    }

    [Fact]
    public async Task Soft_delete_still_applies_alongside_the_tenant_filter()
    {
        var dbName = Guid.NewGuid().ToString();

        await using var context = TestDbContextFactory.Create(tenantId: 1, databaseName: dbName);
        context.Tenants.Add(new Tenant { Id = 1, Name = "Tenant One" });
        var widget = new TestTenantWidget { Name = "Soft deletable" };
        context.TenantWidgets.Add(widget);
        await context.SaveChangesAsync();

        widget.IsDeleted = true;
        await context.SaveChangesAsync();

        Assert.Empty(context.TenantWidgets.ToList());
    }
}
