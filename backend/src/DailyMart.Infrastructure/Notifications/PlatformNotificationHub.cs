using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DailyMart.Infrastructure.Notifications;

/// <summary>
/// Connection endpoint only - the server pushes events to connected clients (SignalRPlatformRealtimeNotifier
/// via IHubContext&lt;PlatformNotificationHub&gt;), clients never call a method on it, so there's nothing to
/// declare here beyond the class itself. [Authorize(Roles = "PlatformAdmin")] mirrors every REST endpoint
/// under api/platform/* - only a platform-admin token can open this connection, enforced the same way
/// (same JWT bearer scheme, same role claim check) even though the token has to arrive via query string
/// here instead of an Authorization header - see Program.cs's JwtBearerEvents.OnMessageReceived.
/// </summary>
[Authorize(Roles = "PlatformAdmin")]
public class PlatformNotificationHub : Hub
{
}
