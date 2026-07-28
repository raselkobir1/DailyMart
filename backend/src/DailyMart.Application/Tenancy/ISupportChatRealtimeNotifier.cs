namespace DailyMart.Application.Tenancy;

/// <summary>
/// Pushes a newly-sent support-chat message live (SignalR, see SupportChatHub in Infrastructure) to
/// whoever's connected to that tenant's conversation right now - the tenant's own connected sessions, and
/// any platform admin currently viewing that specific tenant's chat panel - plus a lightweight
/// tenant-agnostic ping to every connected platform admin, so the Companies list's unread badge can
/// update live even for a tenant nobody's chat panel is currently open on. Implementations must never
/// throw - a connected-client push failure here is no more important than PlatformNotificationHub's own
/// same never-throws requirement (the message itself is already durably saved before this is ever
/// called - see SupportChatService).
/// </summary>
public interface ISupportChatRealtimeNotifier
{
    Task NotifyNewMessageAsync(long tenantId, SupportMessageDto message, CancellationToken cancellationToken = default);
}
