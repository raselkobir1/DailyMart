using DailyMart.API.Filters;
using DailyMart.Application.BetaAnalytics;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>
/// Demo module proving out the per-tenant feature entitlement mechanism end to end (see CLAUDE.md §4's
/// Per-tenant feature entitlement bullet): its Menu row ("beta-analytics") is seeded with
/// IsGenerallyAvailable=false, so no tenant can reach this controller at all until a platform admin
/// grants it via api/platform/tenants/{id}/features/{menuId}/grant. [RequireFeature] is the
/// backend-enforced half of that - even a direct API call from a non-entitled tenant is rejected with
/// 403, not just hidden from the sidebar. This controller is a deliberately trivial demo, not a real
/// analytics feature - remove it (and its Menu seed row/frontend route) once the entitlement mechanism
/// itself has a real first restricted feature to carry this role instead.
/// </summary>
[ApiController]
[Route("api/beta-analytics")]
[RequireFeature("beta-analytics")]
public class BetaAnalyticsController : ControllerBase
{
    private readonly IBetaAnalyticsService _betaAnalyticsService;

    public BetaAnalyticsController(IBetaAnalyticsService betaAnalyticsService)
    {
        _betaAnalyticsService = betaAnalyticsService;
    }

    [HttpGet]
    public async Task<ActionResult<BetaAnalyticsSnapshotDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _betaAnalyticsService.GetSnapshotAsync(cancellationToken));
    }
}
