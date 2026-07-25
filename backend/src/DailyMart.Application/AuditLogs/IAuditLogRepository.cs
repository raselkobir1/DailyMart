using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auditing;

namespace DailyMart.Application.AuditLogs;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>For the entity-type filter dropdown - queried at the DB level (not GetAllAsync + in-memory
    /// Distinct) since the audit log is append-only and can grow far larger than any other table.</summary>
    Task<IReadOnlyList<string>> GetDistinctEntityNamesAsync(CancellationToken cancellationToken = default);
}
