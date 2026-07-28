using DailyMart.Application.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>
/// The only endpoint in this codebase deliberately reachable with no bearer token at all - it backs the
/// public marketing landing page's pricing section (a visitor hasn't signed up yet, so there's no tenant
/// or platform-admin token to send). [AllowAnonymous] overrides the global "any authenticated user"
/// fallback policy (Program.cs) for this one action only. Read-only, and only ever returns active Plans -
/// the exact same list a platform admin sees in the "change plan" dropdown (IPlanService.GetActiveAsync),
/// nothing tenant-specific or sensitive.
/// </summary>
[ApiController]
[Route("api/public/plans")]
[AllowAnonymous]
public class PublicPlansController : ControllerBase
{
    private readonly IPlanService _planService;

    public PublicPlansController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PlanDto>>> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _planService.GetActiveAsync(cancellationToken));
    }
}
