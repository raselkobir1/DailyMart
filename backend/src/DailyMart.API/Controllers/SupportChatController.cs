using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Tenant-side half of the support chat - any signed-in user of the shop (no Admin-only
/// restriction, see CLAUDE.md), always scoped to their own tenant via ICurrentTenantService. The
/// platform-admin half of the same conversation is nested on PlatformTenantsController's {id}/support-chat
/// routes instead, since that side needs an explicit tenantId (a platform token has no tenant context of
/// its own).</summary>
[ApiController]
[Route("api/support-chat")]
public class SupportChatController : ControllerBase
{
    private readonly ISupportChatService _supportChatService;
    private readonly ICurrentTenantService _currentTenantService;

    public SupportChatController(ISupportChatService supportChatService, ICurrentTenantService currentTenantService)
    {
        _supportChatService = supportChatService;
        _currentTenantService = currentTenantService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupportMessageDto>>> GetConversation(
        [FromQuery] int take, CancellationToken cancellationToken)
    {
        return Ok(await _supportChatService.GetConversationAsync(RequireTenantId(), take <= 0 ? 50 : take, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<SupportMessageDto>> Send(
        SendSupportMessageRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _supportChatService.SendFromTenantAsync(RequireTenantId(), request.Message, cancellationToken));
    }

    [HttpPost("read")]
    public async Task<IActionResult> MarkRead(CancellationToken cancellationToken)
    {
        await _supportChatService.MarkReadByTenantAsync(RequireTenantId(), cancellationToken);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await _supportChatService.GetUnreadCountForTenantAsync(RequireTenantId(), cancellationToken));
    }

    /// <summary>Fail-closed like every other tenant-scoped read in this codebase - a null tenant id would
    /// mean a platform-admin token somehow reached this tenant-only controller, which routing/auth should
    /// already prevent; this is defensive, not the expected path.</summary>
    private long RequireTenantId() =>
        _currentTenantService.TenantId ?? throw new AuthenticationFailedException("No tenant context on this request.");
}
