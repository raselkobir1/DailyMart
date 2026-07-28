using DailyMart.Application.Tenancy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Notifications;

public class SignalRPlatformRealtimeNotifier : IPlatformRealtimeNotifier
{
    private readonly IHubContext<PlatformNotificationHub> _hubContext;
    private readonly ILogger<SignalRPlatformRealtimeNotifier> _logger;

    public SignalRPlatformRealtimeNotifier(
        IHubContext<PlatformNotificationHub> hubContext, ILogger<SignalRPlatformRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewTenantSignupAsync(
        long notificationId,
        long tenantId,
        string tenantName,
        string adminUsername,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync(
                "NewTenantSignup",
                new { id = notificationId, tenantId, tenantName, adminUsername, createdAt },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Never propagates - see IPlatformRealtimeNotifier's doc comment. A push with nobody
            // connected isn't even an error case for SignalR (Clients.All is simply a no-op then); this
            // only catches genuine transport failures.
            _logger.LogWarning(ex,
                "Could not push the live signup notification for tenant {TenantId} ({TenantName}).",
                tenantId, tenantName);
        }
    }
}
