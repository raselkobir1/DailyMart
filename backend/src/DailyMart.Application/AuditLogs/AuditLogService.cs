using System.Linq.Expressions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auditing;

namespace DailyMart.Application.AuditLogs;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentTenantService _currentTenantService;

    public AuditLogService(IAuditLogRepository auditLogRepository, ICurrentTenantService currentTenantService)
    {
        _auditLogRepository = auditLogRepository;
        _currentTenantService = currentTenantService;
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        PagedRequest request,
        string? entityName = null,
        AuditAction? action = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // AuditLog doesn't inherit AuditableEntity/TenantOwnedEntity, so it isn't covered by the
        // automatic tenant query filter (see AuditLog's doc comment) - filtered explicitly here instead.
        var tenantId = _currentTenantService.TenantId;

        Expression<Func<AuditLog, bool>> predicate = log =>
            log.TenantId == tenantId &&
            (entityName == null || log.EntityName == entityName) &&
            (action == null || log.Action == action) &&
            (fromDate == null || log.PerformedAt >= fromDate) &&
            (toDate == null || log.PerformedAt <= toDate) &&
            (string.IsNullOrWhiteSpace(request.SearchTerm) ||
                log.PerformedBy.Contains(request.SearchTerm) || log.EntityId.Contains(request.SearchTerm));

        var effectiveRequest = string.IsNullOrWhiteSpace(request.SortBy)
            ? new PagedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SortBy = nameof(AuditLog.PerformedAt),
                SortDescending = true
            }
            : request;

        var result = await _auditLogRepository.GetPagedAsync(effectiveRequest, predicate, cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = result.Items.Select(x => x.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public Task<IReadOnlyList<string>> GetEntityNamesAsync(CancellationToken cancellationToken = default) =>
        _auditLogRepository.GetDistinctEntityNamesAsync(_currentTenantService.TenantId, cancellationToken);
}
