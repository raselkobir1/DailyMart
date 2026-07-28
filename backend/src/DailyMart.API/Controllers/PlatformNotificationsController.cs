using DailyMart.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Platform-operator only, same as PlatformTenantsController - the durable/queryable half of
/// platform notifications (IPlatformNotificationStore), backing the platform-admin panel's bell so a
/// signup that happened while nobody was connected via SignalR is still visible here.</summary>
[ApiController]
[Route("api/platform/notifications")]
[Authorize(Roles = "PlatformAdmin")]
public class PlatformNotificationsController : ControllerBase
{
    private readonly IPlatformNotificationStore _platformNotificationStore;

    public PlatformNotificationsController(IPlatformNotificationStore platformNotificationStore)
    {
        _platformNotificationStore = platformNotificationStore;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PlatformNotificationDto>>> GetRecent(
        [FromQuery] int take, CancellationToken cancellationToken)
    {
        return Ok(await _platformNotificationStore.GetRecentAsync(take <= 0 ? 20 : take, cancellationToken));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await _platformNotificationStore.GetUnreadCountAsync(cancellationToken));
    }

    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken cancellationToken)
    {
        await _platformNotificationStore.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }
}
