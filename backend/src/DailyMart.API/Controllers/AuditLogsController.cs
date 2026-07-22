using DailyMart.Application.AuditLogs;
using DailyMart.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Lists the audit trail captured by Module 0's SaveChanges interceptor. A full browsing/filtering
    /// UI for this belongs to Module 15 (Audit Log UI); this endpoint exists now so the whole Module 0
    /// stack (Service -> DTO -> Validator -> Controller) can be exercised end-to-end.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetPaged(
        [FromQuery] PagedRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogService.GetPagedAsync(request, cancellationToken);
        return Ok(result);
    }
}
