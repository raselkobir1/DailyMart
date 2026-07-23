using DailyMart.Domain.Auditing;
using DailyMart.UnitTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.UnitTests.Persistence;

public class AuditingSaveChangesInterceptorTests
{
    [Fact]
    public async Task Adding_entity_stamps_CreatedAt_and_CreatedBy_and_writes_Created_audit_log()
    {
        await using var context = TestDbContextFactory.Create(new FakeCurrentUserService { UserName = "alice" });

        var widget = new TestWidget { Name = "Widget A" };
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        Assert.NotEqual(default, widget.CreatedAt);
        Assert.Equal("alice", widget.CreatedBy);
        Assert.Null(widget.UpdatedAt);
        Assert.False(widget.IsDeleted);

        var log = Assert.Single(context.AuditLogs.Local);
        Assert.Equal(nameof(TestWidget), log.EntityName);
        Assert.Equal(widget.Id.ToString(), log.EntityId);
        Assert.Equal(AuditAction.Created, log.Action);
        Assert.Equal("alice", log.PerformedBy);
        Assert.Null(log.OldValues);
        Assert.Contains("Widget A", log.NewValues);
    }

    [Fact]
    public async Task Modifying_entity_stamps_UpdatedAt_and_writes_Updated_audit_log_with_changed_columns()
    {
        await using var context = TestDbContextFactory.Create();

        var widget = new TestWidget { Name = "Original" };
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        widget.Name = "Renamed";
        await context.SaveChangesAsync();

        Assert.NotNull(widget.UpdatedAt);
        Assert.Equal("test-user", widget.UpdatedBy);

        var log = context.AuditLogs.Local.Single(l => l.Action == AuditAction.Updated);
        Assert.Contains("Original", log.OldValues);
        Assert.Contains("Renamed", log.NewValues);
        Assert.Contains("Name", log.ChangedColumns);
    }

    /// <summary>
    /// Reproduces the real service pattern (Repository&lt;T&gt;.Update calls DbSet.Update on an entity
    /// that's already tracked, e.g. loaded via GetByIdAsync earlier in the same request) rather than this
    /// suite's other test, which mutates a tracked entity directly without an explicit Update() call.
    /// DbSet.Update() marks the whole entry Modified, which used to make every scalar property report
    /// IsModified = true regardless of whether its value actually changed - ChangedColumns must only ever
    /// list columns whose value is genuinely different, not every column on the entity.
    /// </summary>
    [Fact]
    public async Task ChangedColumns_only_lists_columns_whose_value_actually_changed_even_via_explicit_Update()
    {
        await using var context = TestDbContextFactory.Create();

        var widget = new TestWidget { Name = "Original" };
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        widget.Name = "Renamed";
        context.Widgets.Update(widget);
        await context.SaveChangesAsync();

        var log = context.AuditLogs.Local.Single(l => l.Action == AuditAction.Updated);
        Assert.Contains("Name", log.ChangedColumns);
        Assert.DoesNotContain("CreatedAt", log.ChangedColumns);
        Assert.DoesNotContain("CreatedBy", log.ChangedColumns);
        Assert.DoesNotContain("IsDeleted", log.ChangedColumns);
    }

    [Fact]
    public async Task Removing_entity_converts_to_soft_delete_instead_of_a_real_delete()
    {
        await using var context = TestDbContextFactory.Create();

        var widget = new TestWidget { Name = "To be removed" };
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        context.Widgets.Remove(widget);
        await context.SaveChangesAsync();

        Assert.True(widget.IsDeleted);
        Assert.Equal(EntityState.Unchanged, context.Entry(widget).State);

        var log = context.AuditLogs.Local.Single(l => l.Action == AuditAction.Deleted);
        Assert.Equal(widget.Id.ToString(), log.EntityId);
    }

    [Fact]
    public async Task Soft_deleted_entity_is_excluded_from_default_queries_but_still_in_the_database()
    {
        await using var context = TestDbContextFactory.Create();

        var widget = new TestWidget { Name = "Hidden after delete" };
        context.Widgets.Add(widget);
        await context.SaveChangesAsync();

        context.Widgets.Remove(widget);
        await context.SaveChangesAsync();

        Assert.Empty(await context.Widgets.ToListAsync());
        Assert.Single(await context.Widgets.IgnoreQueryFilters().ToListAsync());
    }
}
