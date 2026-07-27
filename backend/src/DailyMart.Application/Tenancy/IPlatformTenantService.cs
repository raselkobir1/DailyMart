using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Tenancy;

/// <summary>
/// The "basic" platform-admin panel's read/suspend surface over the Tenant list - not tenant/company
/// data itself (products, sales, etc.), just the Tenant rows. Deliberately no create/edit/delete:
/// tenants are only ever created via self-service registration (ITenantProvisioningService), and
/// suspending (not deleting) is the only lifecycle action this phase needs.
/// </summary>
public interface IPlatformTenantService
{
    /// <summary>status: "active"|"suspended"|null(all). billingStatus: "overdue"|"paid"|"free"|null(all).
    /// Sorting/filtering happens across every tenant (see PlatformTenantService's doc comment on why),
    /// so PagedRequest.SortBy/SortDescending work against any TenantSummaryDto field, not just Tenant's
    /// own columns.</summary>
    Task<PagedResult<TenantSummaryDto>> GetPagedAsync(
        PagedRequest request, string? status = null, string? billingStatus = null, CancellationToken cancellationToken = default);

    Task<TenantSummaryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<TenantSummaryDto> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
}
