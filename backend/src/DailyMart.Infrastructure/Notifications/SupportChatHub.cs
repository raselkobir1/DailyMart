using DailyMart.Application.Common.Interfaces;
using DailyMart.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DailyMart.Infrastructure.Notifications;

/// <summary>
/// Unlike PlatformNotificationHub ([Authorize(Roles = "PlatformAdmin")] only), this hub is reachable by
/// BOTH a tenant-authenticated user and a platform admin - a support conversation has to be. Plain
/// [Authorize] (any authenticated identity, matching the global fallback policy) is enough; which group(s)
/// a connection joins depends on what its own token carries, decided here rather than by a separate role
/// check on every message send.
///
/// A tenant user is auto-joined to their own tenant's room on connect (their tenant_id claim never
/// changes mid-connection, so there's nothing for their client to explicitly request). A platform admin
/// isn't auto-joined to any tenant's room - they might view several tenants' conversations in one
/// session - so their client calls JoinTenantConversation/LeaveTenantConversation as they open/close a
/// specific tenant's chat panel. Every platform admin is also joined to a shared "platform-admins" group,
/// used for the tenant-agnostic "some tenant has a new unread message" ping that keeps the Companies
/// list's unread badges live even for a tenant nobody's chat panel is currently open on.
/// </summary>
[Authorize]
public class SupportChatHub : Hub
{
    public const string PlatformAdminsGroupName = "platform-admins";

    public static string TenantGroupName(long tenantId) => $"tenant-{tenantId}";

    public override async Task OnConnectedAsync()
    {
        var tenantIdClaim = Context.User?.FindFirst(ICurrentTenantService.ClaimType)?.Value;
        if (tenantIdClaim is not null && long.TryParse(tenantIdClaim, out var tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroupName(tenantId));
        }
        else if (Context.User.IsGenuinePlatformAdmin())
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PlatformAdminsGroupName);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>No-op for a tenant user (already in their own room, and has no business joining another
    /// tenant's) - only a platform-admin connection actually moves groups here.</summary>
    public async Task JoinTenantConversation(long tenantId)
    {
        if (Context.User.IsGenuinePlatformAdmin())
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroupName(tenantId));
        }
    }

    public async Task LeaveTenantConversation(long tenantId)
    {
        if (Context.User.IsGenuinePlatformAdmin())
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, TenantGroupName(tenantId));
        }
    }
}
