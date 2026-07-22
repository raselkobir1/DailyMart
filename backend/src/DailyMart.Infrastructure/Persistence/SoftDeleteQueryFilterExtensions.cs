using System.Linq.Expressions;
using DailyMart.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence;

public static class SoftDeleteQueryFilterExtensions
{
    /// <summary>
    /// Applies a global `IsDeleted == false` query filter to every entity type in the model that
    /// derives from <see cref="AuditableEntity"/>. Extracted out of DailyMartDbContext.OnModelCreating
    /// so the same convention can be exercised against a throwaway test entity in unit tests.
    /// </summary>
    public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(BuildSoftDeleteFilter(entityType.ClrType));
            }
        }
    }

    private static LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "entity");
        var property = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
        var condition = Expression.Equal(property, Expression.Constant(false));
        return Expression.Lambda(condition, parameter);
    }
}
