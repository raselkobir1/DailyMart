namespace DailyMart.Application.Reports;

public interface IReportService
{
    /// <summary>date's calendar day/month/year (in UTC) determines the report's [from, to] range - e.g.
    /// Month with any date in July 2026 reports on all of July 2026, not a rolling 30 days.</summary>
    Task<ClosingReportDto> GetClosingReportAsync(
        ClosingReportPeriod period, DateOnly date, CancellationToken cancellationToken = default);
}
