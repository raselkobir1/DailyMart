using DailyMart.Application.Billing;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Platform-operator only - lists/manages Tenant rows themselves, not any one tenant's
/// business data. [Authorize(Roles = "PlatformAdmin")] works the same way [Authorize(Roles = "Admin")]
/// already does for Users/Roles/Menus - the global JWT bearer scheme, just checking a different role
/// claim value. A regular tenant User's token can never carry "PlatformAdmin", so this is naturally
/// exclusive to platform-admin tokens. Also carries the {id}/subscription sub-routes (billing) - nested
/// resource in the same controller, matching PurchasesController's {id}/returns convention rather than a
/// separate controller.</summary>
[ApiController]
[Route("api/platform/tenants")]
[Authorize(Roles = "PlatformAdmin")]
public class PlatformTenantsController : ControllerBase
{
    private readonly IPlatformTenantService _platformTenantService;
    private readonly ISubscriptionService _subscriptionService;

    public PlatformTenantsController(
        IPlatformTenantService platformTenantService, ISubscriptionService subscriptionService)
    {
        _platformTenantService = platformTenantService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TenantSummaryDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _platformTenantService.GetPagedAsync(request, cancellationToken));
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
}
