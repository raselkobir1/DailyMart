using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auditing;

namespace DailyMart.Application.AuditLogs;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>For the entity-type filter dropdown - queried at the DB level (not GetAllAsync + in-memory
    /// Distinct) since the audit log is append-only and can grow far larger than any other table.
    /// tenantId is required (not read off the DbContext automatically) - AuditLog doesn't inherit
    /// AuditableEntity/TenantOwnedEntity, so it isn't covered by the automatic tenant query filter;
    /// see AuditLog's own doc comment.</summary>
    Task<IReadOnlyList<string>> GetDistinctEntityNamesAsync(long? tenantId, CancellationToken cancellationToken = default);
}
