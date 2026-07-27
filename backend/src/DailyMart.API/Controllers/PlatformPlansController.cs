using DailyMart.Application.Billing;
using DailyMart.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

/// <summary>Platform-operator only - manages the Plan catalog (Free/Basic/Pro/...) tenants can be put
/// on. See PlatformTenantsController's doc comment for the same [Authorize(Roles = "PlatformAdmin")]
/// reasoning.</summary>
[ApiController]
[Route("api/platform/plans")]
[Authorize(Roles = "PlatformAdmin")]
public class PlatformPlansController : ControllerBase
{
    private readonly IPlanService _planService;

    public PlatformPlansController(IPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PlanDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _planService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<PlanDto>>> GetActive(CancellationToken cancellationToken)
    {
        return Ok(await _planService.GetActiveAsync(cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PlanDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _planService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PlanDto>> Create(PlanRequestDto request, CancellationToken cancellationToken)
    {
        var plan = await _planService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<PlanDto>> Update(long id, PlanRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _planService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/activate")]
    public async Task<ActionResult<PlanDto>> Activate(long id, CancellationToken cancellationToken)
    {
        return Ok(await _planService.ActivateAsync(id, cancellationToken));
    }

    [HttpPost("{id:long}/deactivate")]
    public async Task<ActionResult<PlanDto>> Deactivate(long id, CancellationToken cancellationToken)
    {
        return Ok(await _planService.DeactivateAsync(id, cancellationToken));
    }
}
