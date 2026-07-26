namespace DailyMart.Infrastructure.Persistence;

/// <summary>
/// Minimal contract TenancyModelExtensions needs from a DbContext to build the tenant query filter -
/// split out so the convention itself can be exercised in unit tests against a throwaway test
/// context, the same way SoftDeleteQueryFilterExtensions is tested against TestDbContext/TestWidget,
/// rather than only trusting it by inspection against the real DailyMartDbContext.
/// </summary>
public interface ITenantScopedDbContext
{
    long? CurrentTenantId { get; }
}
