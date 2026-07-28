using DailyMart.Application.Tenancy;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DailyMart.Infrastructure.Notifications;

public class SignalRSupportChatRealtimeNotifier : ISupportChatRealtimeNotifier
{
    private readonly IHubContext<SupportChatHub> _hubContext;
    private readonly ILogger<SignalRSupportChatRealtimeNotifier> _logger;

    public SignalRSupportChatRealtimeNotifier(
        IHubContext<SupportChatHub> hubContext, ILogger<SignalRSupportChatRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewMessageAsync(
        long tenantId, SupportMessageDto message, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(SupportChatHub.TenantGroupName(tenantId))
                .SendAsync("NewSupportMessage", message, cancellationToken);

            // Tenant-agnostic ping so a platform admin NOT currently viewing this tenant's chat panel
            // still sees its unread badge update live on the Companies list.
            await _hubContext.Clients.Group(SupportChatHub.PlatformAdminsGroupName)
                .SendAsync("SupportChatUpdated", new { tenantId }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not push the live support-chat message for tenant {TenantId}.", tenantId);
        }
    }
}
