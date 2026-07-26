using DailyMart.Application.AuditLogs;
using DailyMart.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(DbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<string>> GetDistinctEntityNamesAsync(
        long? tenantId, CancellationToken cancellationToken = default) =>
        await Entities
            .Where(a => a.TenantId == tenantId)
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);
}
