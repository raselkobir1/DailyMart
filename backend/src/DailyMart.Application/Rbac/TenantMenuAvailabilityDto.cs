namespace DailyMart.Application.Rbac;

/// <summary>One row per current Menu, denormalized with whether a specific tenant can see it and why -
/// powers the platform-admin "features" screen (GET /api/platform/tenants/{id}/features). Generally
/// available menus are included for transparency (so the screen can show "available to everyone") but
/// IsGranted/grant-revoke only ever applies to restricted ones.</summary>
public class TenantMenuAvailabilityDto
{
    public long MenuId { get; init; }

    public string MenuKey { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public long? ParentId { get; init; }

    public int SortOrder { get; init; }

    /// <summary>True means every tenant gets this menu automatically - nothing to grant/revoke here.</summary>
    public bool IsGenerallyAvailable { get; init; }

    /// <summary>True if this tenant holds an explicit TenantFeatureGrant for this menu. Meaningless
    /// (always false) when IsGenerallyAvailable is true, since no grant row is ever created for those.</summary>
    public bool IsGranted { get; init; }

    /// <summary>IsGenerallyAvailable || IsGranted - whether this tenant can actually see the menu today.</summary>
    public bool IsAvailable { get; init; }
}
