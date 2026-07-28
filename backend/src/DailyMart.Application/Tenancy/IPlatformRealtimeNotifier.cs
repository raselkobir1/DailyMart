namespace DailyMart.Application.Tenancy;

/// <summary>
/// Pushes a live event to every currently-connected platform-admin browser session (SignalR, see
/// PlatformNotificationHub in Infrastructure) - a second, complementary channel alongside
/// IPlatformNotificationService's email, for an admin who's actively watching the panel right now rather
/// than checking their inbox later. Implementations must never throw - a connected-client push failure
/// is exactly as unimportant to the caller (a new tenant's registration) as an email failure is; see
/// PlatformNotificationService for how both channels are combined without letting either one block
/// registration or fail the other.
/// </summary>
public interface IPlatformRealtimeNotifier
{
    /// <summary>notificationId is the PlatformNotification row's real database Id (see
    /// IPlatformNotificationStore.RecordNewTenantSignupAsync) - pushed as-is so a connected client's
    /// live event and a later history fetch both refer to the exact same notification identity.</summary>
    Task NotifyNewTenantSignupAsync(
        long notificationId,
        long tenantId,
        string tenantName,
        string adminUsername,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);
}
