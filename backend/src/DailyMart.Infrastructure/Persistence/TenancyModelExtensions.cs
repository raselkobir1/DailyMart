using System.Linq.Expressions;
using DailyMart.Domain.Common;
using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence;

/// <summary>
/// The two model-wide conventions that make every TenantOwnedEntity actually tenant-isolated,
/// applied once here rather than repeated per module - same spirit as
/// SoftDeleteQueryFilterExtensions, which this replaces (not supplements) for DailyMartDbContext:
/// EF Core only allows one HasQueryFilter per entity type, so the combined
/// "!IsDeleted &amp;&amp; TenantId == CurrentTenantId" predicate has to be built as a single filter
/// rather than two separate calls. TestDbContext (soft-delete-only, no tenant concept) keeps using
/// SoftDeleteQueryFilterExtensions unchanged.
/// </summary>
public static class TenancyModelExtensions
{
    /// <summary>FK from every TenantOwnedEntity to Tenant, via the property-name overload since
    /// there's no CLR navigation property on either side (an entity doesn't need a `Tenant Tenant`
    /// property just to satisfy this constraint).</summary>
    public static void ApplyTenantForeignKeys(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TenantOwnedEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(typeof(Tenant))
                    .WithMany()
                    .HasForeignKey(nameof(TenantOwnedEntity.TenantId))
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }

    /// <summary>
    /// Applies "!IsDeleted" to every AuditableEntity, plus "&amp;&amp; TenantId == dbContext.CurrentTenantId"
    /// to every TenantOwnedEntity. The filter closes over the live DbContext instance (captured as a
    /// constant in the expression tree, the standard EF Core pattern for a per-request-varying filter
    /// value) so it re-evaluates CurrentTenantId on every query, not just once at model-build time.
    ///
    /// Deliberately fail-closed: when CurrentTenantId is null (a platform-admin token, or any other
    /// anomaly), the filter becomes "TenantId == null", which EF translates to a real SQL null
    /// comparison - matching zero rows on any tenant-scoped table, since the column is NOT NULL
    /// there. A platform-admin token hitting an ordinary business endpoint sees nothing, never
    /// "every tenant's data."
    /// </summary>
    public static void ApplyTenancyQueryFilters(this ModelBuilder modelBuilder, ITenantScopedDbContext dbContext)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(BuildFilter(entityType.ClrType, dbContext));
            }
        }
    }

    private static LambdaExpression BuildFilter(Type entityType, ITenantScopedDbContext dbContext)
    {
        var parameter = Expression.Parameter(entityType, "entity");
        var isDeletedProperty = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
        Expression condition = Expression.Equal(isDeletedProperty, Expression.Constant(false));

        if (typeof(TenantOwnedEntity).IsAssignableFrom(entityType))
        {
            var tenantIdProperty = Expression.Property(parameter, nameof(TenantOwnedEntity.TenantId));
            var currentTenantIdProperty = Expression.Property(
                Expression.Constant(dbContext, typeof(ITenantScopedDbContext)), nameof(ITenantScopedDbContext.CurrentTenantId));
            var tenantCondition = Expression.Equal(
                Expression.Convert(tenantIdProperty, typeof(long?)), currentTenantIdProperty);
            condition = Expression.AndAlso(condition, tenantCondition);
        }

        return Expression.Lambda(condition, parameter);
    }
}
