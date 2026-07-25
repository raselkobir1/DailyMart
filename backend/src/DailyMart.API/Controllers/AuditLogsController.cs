using DailyMart.Application.AuditLogs;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Auditing;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Module 15's browsing/filtering UI over the audit trail Module 0's SaveChanges interceptor
/// captures for every module.</summary>
[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetPaged(
        [FromQuery] PagedRequest request,
        [FromQuery] string? entityName,
        [FromQuery] AuditAction? action,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetPagedAsync(request, entityName, action, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    /// <summary>Backs the entity-type filter dropdown - distinct EntityName values actually present in
    /// the log, so the list can never drift from what's really been audited.</summary>
    [HttpGet("entity-names")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetEntityNames(CancellationToken cancellationToken)
    {
        return Ok(await _auditLogService.GetEntityNamesAsync(cancellationToken));
    }
}
