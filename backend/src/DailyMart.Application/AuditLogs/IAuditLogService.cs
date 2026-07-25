using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auditing;

namespace DailyMart.Application.AuditLogs;

/// <summary>Read-only access to the audit trail captured by Module 0's SaveChanges interceptor.
/// entityName/action/fromDate/toDate are optional filters, mirroring IExpenseService.GetPagedAsync's
/// optional-filter-parameters pattern rather than growing PagedRequest with module-specific fields.
/// request.SearchTerm additionally matches PerformedBy or EntityId, for "what did this user change" /
/// "what happened to this record" lookups.</summary>
public interface IAuditLogService
{
    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        PagedRequest request,
        string? entityName = null,
        AuditAction? action = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>Distinct EntityName values actually present in the log, for populating the entity-type
    /// filter dropdown without hardcoding a list that would drift from what's really been audited.</summary>
    Task<IReadOnlyList<string>> GetEntityNamesAsync(CancellationToken cancellationToken = default);
}
