namespace DailyMart.Application.Tenancy;

/// <summary>
/// Reads one specific tenant's contact email (ShopSettings.ShopEmail) from platform-admin context, where
/// there is no "current tenant" for the ordinary tenant-scoped query filter to key off of. Implemented in
/// Infrastructure, not Application, for the same reason as ITenantProvisioningService/
/// IUsageAnalyticsService - see TenantContactLookupService's doc comment.
/// </summary>
public interface ITenantContactLookupService
{
    /// <summary>Null if the tenant has no ShopSettings.ShopEmail on file - which is common for a
    /// brand-new or Free-plan tenant, since it's optional and only ever set by the tenant's own Admin
    /// visiting their Settings page (never populated at signup). Callers must treat null as "not
    /// reachable yet," not an error.</summary>
    Task<string?> GetShopEmailAsync(long tenantId, CancellationToken cancellationToken = default);
}
