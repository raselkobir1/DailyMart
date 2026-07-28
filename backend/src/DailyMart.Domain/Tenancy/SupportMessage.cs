using DailyMart.Domain.Common;

namespace DailyMart.Domain.Tenancy;

/// <summary>
/// One row per message in a tenant's single ongoing support conversation with the platform - global,
/// like PlatformNotification/Tenant itself, since a platform admin (no tenant context) needs to read/
/// write across the tenant boundary. Who sent it is CreatedBy (inherited from AuditableEntity, already
/// auto-stamped by the audit interceptor from whichever identity - tenant user or platform admin - is
/// authenticated for that request) rather than a separate field; FromPlatformAdmin is still needed
/// alongside it purely to know which unread flag to flip, since CreatedBy alone doesn't say which side
/// of the conversation a username belongs to.
/// </summary>
public class SupportMessage : AuditableEntity
{
    public long TenantId { get; set; }

    public bool FromPlatformAdmin { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Always true for a tenant-sent message (you've obviously "read" your own message) - only
    /// meaningful as false for a platform-admin-sent message the tenant hasn't opened yet.</summary>
    public bool IsReadByTenant { get; set; }

    /// <summary>Mirror of IsReadByTenant for the other side.</summary>
    public bool IsReadByPlatformAdmin { get; set; }
}
