using DailyMart.Application.ProfitLoss;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/profit-loss")]
public class ProfitLossController : ControllerBase
{
    private readonly IProfitLossService _profitLossService;

    public ProfitLossController(IProfitLossService profitLossService)
    {
        _profitLossService = profitLossService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ProfitLossSummaryDto>> GetSummary(
        [FromQuery] DateTimeOffset fromDate, [FromQuery] DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        return Ok(await _profitLossService.GetSummaryAsync(fromDate, toDate, cancellationToken));
    }
}
