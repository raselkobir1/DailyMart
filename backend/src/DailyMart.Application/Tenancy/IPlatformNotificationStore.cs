namespace DailyMart.Application.Tenancy;

/// <summary>
/// The durable, queryable half of platform notifications (see PlatformNotification's doc comment) -
/// what backs the platform-admin panel's bell so a signup that happened while nobody was connected via
/// SignalR is still there the next time someone opens the panel. IPlatformRealtimeNotifier is the live,
/// in-the-moment half; both are driven from the same PlatformNotificationService call.
/// </summary>
public interface IPlatformNotificationStore
{
    /// <summary>Creates the row and returns it (with its real database Id) so the caller can pass that
    /// same Id into the live SignalR push - keeping one consistent identity for a notification whether a
    /// client learns about it live or by fetching history later.</summary>
    Task<PlatformNotificationDto> RecordNewTenantSignupAsync(
        long tenantId, string tenantName, string adminUsername, CancellationToken cancellationToken = default);

    /// <summary>Most recent first, capped at <paramref name="take"/> - this is a bell dropdown, not a
    /// full audit log; there's no paging beyond this cap.</summary>
    Task<IReadOnlyList<PlatformNotificationDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    /// <summary>No-op if already read or the id doesn't exist - marking read is idempotent, not an
    /// error-prone action a caller needs to guard.</summary>
    Task MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default);
}
