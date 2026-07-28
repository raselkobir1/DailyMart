using DailyMart.Application.Billing;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Rbac;
using DailyMart.Application.Tenancy;
using DailyMart.Application.UsageAnalytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Platform-operator only - lists/manages Tenant rows themselves, not any one tenant's
/// business data. [Authorize(Roles = "PlatformAdmin")] works the same way [Authorize(Roles = "Admin")]
/// already does for Users/Roles/Menus - the global JWT bearer scheme, just checking a different role
/// claim value. A regular tenant User's token can never carry "PlatformAdmin", so this is naturally
/// exclusive to platform-admin tokens. Also carries the {id}/subscription (billing), {id}/usage, and
/// {id}/features (per-tenant feature entitlement - see IFeatureEntitlementService) sub-routes - nested
/// resources in the same controller, matching PurchasesController's {id}/returns convention rather than
/// a separate controller per concern.</summary>
[ApiController]
[Route("api/platform/tenants")]
[Authorize(Roles = "PlatformAdmin")]
public class PlatformTenantsController : ControllerBase
{
    private readonly IPlatformTenantService _platformTenantService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageAnalyticsService _usageAnalyticsService;
    private readonly IFeatureEntitlementService _featureEntitlementService;

    public PlatformTenantsController(
        IPlatformTenantService platformTenantService,
        ISubscriptionService subscriptionService,
        IUsageAnalyticsService usageAnalyticsService,
        IFeatureEntitlementService featureEntitlementService)
    {
        _platformTenantService = platformTenantService;
        _subscriptionService = subscriptionService;
        _usageAnalyticsService = usageAnalyticsService;
        _featureEntitlementService = featureEntitlementService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TenantSummaryDto>>> GetPaged(
        [FromQuery] PagedRequest request,
        [FromQuery] string? status,
        [FromQuery] string? billingStatus,
        CancellationToken cancellationToken)
    {
        return Ok(await _platformTenantService.GetPagedAsync(request, status, billingStatus, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TenantSummaryDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _platformTenantService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/activate")]
    public async Task<ActionResult<TenantSummaryDto>> Activate(long id, CancellationToken cancellationToken)
    {
        return Ok(await _platformTenantService.SetActiveAsync(id, isActive: true, cancellationToken));
    }

    [HttpPost("{id:long}/suspend")]
    public async Task<ActionResult<TenantSummaryDto>> Suspend(long id, CancellationToken cancellationToken)
    {
        return Ok(await _platformTenantService.SetActiveAsync(id, isActive: false, cancellationToken));
    }

    [HttpGet("{id:long}/subscription")]
    public async Task<ActionResult<TenantSubscriptionDto>> GetSubscription(long id, CancellationToken cancellationToken)
    {
        return Ok(await _subscriptionService.GetByTenantIdAsync(id, cancellationToken));
    }

    [HttpGet("{id:long}/subscription/payments")]
    public async Task<ActionResult<PagedResult<SubscriptionPaymentDto>>> GetPaymentHistory(
        long id, [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _subscriptionService.GetPaymentHistoryAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/subscription/change-plan")]
    public async Task<ActionResult<TenantSubscriptionDto>> ChangePlan(
        long id, ChangePlanRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _subscriptionService.ChangePlanAsync(id, request.PlanId, cancellationToken));
    }

    [HttpPost("{id:long}/subscription/payments")]
    public async Task<ActionResult<SubscriptionPaymentDto>> RecordPayment(
        long id, RecordPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var payment = await _subscriptionService.RecordPaymentAsync(id, request, cancellationToken);
        return Ok(payment);
    }

    [HttpGet("{id:long}/usage")]
    public async Task<ActionResult<TenantUsageSnapshotDto>> GetUsage(long id, CancellationToken cancellationToken)
    {
        var snapshots = await _usageAnalyticsService.GetSnapshotsByTenantIdsAsync([id], cancellationToken);
        return Ok(snapshots[id]);
    }

    [HttpGet("{id:long}/features")]
    public async Task<ActionResult<IReadOnlyList<TenantMenuAvailabilityDto>>> GetFeatures(long id, CancellationToken cancellationToken)
    {
        return Ok(await _featureEntitlementService.GetMenuAvailabilityForTenantAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/features/{menuId:long}/grant")]
    public async Task<IActionResult> GrantFeature(long id, long menuId, CancellationToken cancellationToken)
    {
        await _featureEntitlementService.GrantAsync(id, menuId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/features/{menuId:long}/revoke")]
    public async Task<IActionResult> RevokeFeature(long id, long menuId, CancellationToken cancellationToken)
    {
        await _featureEntitlementService.RevokeAsync(id, menuId, cancellationToken);
        return NoContent();
    }
}
