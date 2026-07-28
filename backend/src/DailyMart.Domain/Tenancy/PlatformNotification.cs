using DailyMart.Domain.Common;

namespace DailyMart.Domain.Tenancy;

/// <summary>
/// A durable record of a platform-level event (currently only "a new tenant signed up") - the persisted
/// counterpart to the live SignalR push (PlatformNotificationHub/IPlatformRealtimeNotifier). Global, like
/// Tenant/PlatformAdmin itself: this isn't owned by the tenant it's about (a suspended/deleted tenant's
/// signup notification should still be readable), and platform admins aren't tenant-scoped either.
///
/// IsRead is a single shared flag, not per-admin - there's currently no way to create more than one
/// PlatformAdmin account (no PlatformAdminsController), so this panel is a single-operator "basic" tool
/// per CLAUDE.md, and a shared read flag matches that scope; move to a per-admin read-receipt table only
/// if/when multiple platform admins become a real scenario.
/// </summary>
public class PlatformNotification : AuditableEntity
{
    /// <summary>Always "NewTenantSignup" today - a string, not an enum, so a future notification type
    /// doesn't need a migration to add.</summary>
    public string Type { get; set; } = string.Empty;

    public long? TenantId { get; set; }

    /// <summary>Denormalized snapshot at the time of the event, same reasoning as
    /// TenantSubscriptionDto.TenantName - a later tenant rename shouldn't rewrite history.</summary>
    public string TenantName { get; set; } = string.Empty;

    public string? AdminUsername { get; set; }

    public bool IsRead { get; set; }
}
