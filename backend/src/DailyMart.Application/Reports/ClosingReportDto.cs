namespace DailyMart.Application.Reports;

/// <summary>
/// The BRD's "Daily/Monthly/Yearly Closing Report" - a period-end snapshot, distinct from Profit &amp;
/// Loss (Module 13, an arbitrary custom range) and the Dashboard (Module 16, always "today"). Deliberately
/// omits Customer/Supplier due and low-stock figures - those are running balances, not period-scoped flows,
/// and reconstructing a true point-in-time historical balance is out of MVP scope; the Dashboard already
/// shows the current live balance.
/// </summary>
public class ClosingReportDto
{
    public string PeriodType { get; init; } = string.Empty;

    public DateTimeOffset FromDate { get; init; }

    public DateTimeOffset ToDate { get; init; }

    public decimal Revenue { get; init; }

    public int SalesCount { get; init; }

    public decimal Cogs { get; init; }

    public decimal GrossProfit { get; init; }

    public decimal TotalPurchases { get; init; }

    public int PurchasesCount { get; init; }

    public decimal TotalExpenses { get; init; }

    public decimal NetProfit { get; init; }

    /// <summary>Sum of Sale.PaidAmount in the period.</summary>
    public decimal CashIn { get; init; }

    /// <summary>Sum of Purchase.PaidAmount + Expense.Amount in the period.</summary>
    public decimal CashOut { get; init; }

    public decimal NetCashFlow { get; init; }
}
