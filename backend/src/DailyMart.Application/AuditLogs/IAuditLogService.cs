using DailyMart.Application.Common.Models;

namespace DailyMart.Application.AuditLogs;

/// <summary>Read-only access to the audit trail captured by Module 0's SaveChanges interceptor.</summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);
}
