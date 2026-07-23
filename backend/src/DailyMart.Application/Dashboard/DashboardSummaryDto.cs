namespace DailyMart.Application.Dashboard;

public class DashboardSummaryDto
{
    public decimal TodaySales { get; set; }

    public decimal TodayPurchases { get; set; }

    public decimal TodayProfit { get; set; }

    /// <summary>Always 0 until Module 12 (Expense) exists - there is nowhere yet to record an expense
    /// against. Kept on the DTO now so the frontend card layout/BRD field doesn't need to change later.</summary>
    public decimal TodayExpense { get; set; }

    /// <summary>Sum of all-time Sale.PaidAmount minus all-time Purchase.PaidAmount minus TodayExpense (0 for
    /// now). An MVP cash-position figure - there's no separate cash-ledger table, so this is always
    /// recomputed rather than a stored running balance.</summary>
    public decimal CashInHand { get; set; }

    public decimal TotalCustomerDue { get; set; }

    public decimal TotalSupplierDue { get; set; }

    /// <summary>Sum of CurrentStock * PurchasePrice across every product - valued at cost, not selling price.</summary>
    public decimal InventoryValue { get; set; }

    public int LowStockCount { get; set; }

    public IReadOnlyList<LowStockProductDto> LowStockProducts { get; set; } = [];

    /// <summary>Best sellers by quantity over the last 30 days, not all-time - keeps the list relevant to
    /// what's currently moving rather than being dominated by long-discontinued products.</summary>
    public IReadOnlyList<TopSellingProductDto> TopSellingProducts { get; set; } = [];

    /// <summary>Last 7 calendar days (oldest first, today last) for the dashboard's sales/purchases/profit
    /// chart.</summary>
    public IReadOnlyList<DashboardTrendPointDto> SalesTrend { get; set; } = [];
}
