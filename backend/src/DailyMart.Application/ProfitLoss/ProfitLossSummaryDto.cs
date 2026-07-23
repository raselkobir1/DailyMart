namespace DailyMart.Application.ProfitLoss;

public class ProfitLossSummaryDto
{
    public DateTimeOffset FromDate { get; init; }

    public DateTimeOffset ToDate { get; init; }

    public decimal Revenue { get; init; }

    /// <summary>Cost of Goods Sold - the sum of each sale's TotalCost snapshot (Module 9), not derived
    /// from Purchase totals. COGS reflects the cost of what was actually SOLD in the period, which is why
    /// it's read from Sale, not Purchase.</summary>
    public decimal Cogs { get; init; }

    public decimal GrossProfit { get; init; }

    public decimal OperatingExpense { get; init; }

    public decimal NetProfit { get; init; }
}
