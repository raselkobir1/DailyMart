using DailyMart.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("closing")]
    public async Task<ActionResult<ClosingReportDto>> GetClosingReport(
        [FromQuery] ClosingReportPeriod period, [FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetClosingReportAsync(period, date, cancellationToken));
    }
}
