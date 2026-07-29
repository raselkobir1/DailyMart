using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DailyMart.Infrastructure.Notifications;

/// <summary>
/// Connection endpoint only - the server pushes events to connected clients (SignalRPlatformRealtimeNotifier
/// via IHubContext&lt;PlatformNotificationHub&gt;), clients never call a method on it, so there's nothing to
/// declare here beyond the class itself. [Authorize(Policy = "PlatformAdminOnly")] mirrors every REST
/// endpoint under api/platform/* - see ClaimsPrincipalExtensions.IsGenuinePlatformAdmin for why a bare
/// role check isn't enough - even though the token has to arrive via query string here instead of an
/// Authorization header - see Program.cs's JwtBearerEvents.OnMessageReceived.
/// </summary>
[Authorize(Policy = "PlatformAdminOnly")]
public class PlatformNotificationHub : Hub
{
}
