namespace DailyMart.Application.Tenancy;

/// <summary>
/// One ongoing conversation per tenant with the platform - any signed-in user of that shop can send/read
/// on the tenant side (not Admin-only; see CLAUDE.md's Support chat bullet), and any platform admin can
/// send/read on the platform side, scoped to whichever tenant they're currently viewing. TenantId is
/// always supplied by the caller (the tenant-side controller resolves its own from
/// ICurrentTenantService; the platform-side controller takes it from the route) rather than this service
/// inferring "current tenant" itself, so the same methods serve both directions without duplicating logic.
/// </summary>
public interface ISupportChatService
{
    Task<SupportMessageDto> SendFromTenantAsync(long tenantId, string message, CancellationToken cancellationToken = default);

    Task<SupportMessageDto> SendFromPlatformAdminAsync(long tenantId, string message, CancellationToken cancellationToken = default);

    /// <summary>Chronological (oldest first, ready to render top-to-bottom), most recent
    /// <paramref name="take"/> messages - a chat panel, not a full history browser.</summary>
    Task<IReadOnlyList<SupportMessageDto>> GetConversationAsync(
        long tenantId, int take, CancellationToken cancellationToken = default);

    /// <summary>Messages from the platform admin this tenant hasn't read yet - backs the tenant-side chat
    /// bubble's badge.</summary>
    Task<int> GetUnreadCountForTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>Messages from this tenant the platform admin hasn't read yet - backs the chat panel on
    /// that tenant's platform-admin detail page.</summary>
    Task<int> GetUnreadCountForPlatformAdminAsync(long tenantId, CancellationToken cancellationToken = default);

    /// <summary>Batch form of the above, for the Companies list's unread-badge column - same
    /// "enrich every row without a per-row round trip" shape as ISubscriptionService.GetSummariesByTenantIdsAsync.</summary>
    Task<Dictionary<long, int>> GetUnreadCountsForPlatformAdminAsync(
        IEnumerable<long> tenantIds, CancellationToken cancellationToken = default);

    Task MarkReadByTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    Task MarkReadByPlatformAdminAsync(long tenantId, CancellationToken cancellationToken = default);
}
