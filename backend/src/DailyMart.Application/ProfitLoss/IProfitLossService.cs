namespace DailyMart.Application.ProfitLoss;

public interface IProfitLossService
{
    /// <summary>Revenue/COGS/Gross Profit come from Sale (TotalAmount/TotalCost) within
    /// [fromDate, toDate]; Operating Expense comes from Expense.Amount in the same range. The BRD's
    /// "Daily/Weekly/Monthly/Yearly" requirement is satisfied by the caller choosing the date range
    /// (e.g. the frontend's period-preset buttons), not by separate per-granularity endpoints.</summary>
    Task<ProfitLossSummaryDto> GetSummaryAsync(
        DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken = default);
}
