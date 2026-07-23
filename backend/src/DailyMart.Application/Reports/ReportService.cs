using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.ProfitLoss;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Sales;

namespace DailyMart.Application.Reports;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProfitLossService _profitLossService;

    public ReportService(IUnitOfWork unitOfWork, IProfitLossService profitLossService)
    {
        _unitOfWork = unitOfWork;
        _profitLossService = profitLossService;
    }

    public async Task<ClosingReportDto> GetClosingReportAsync(
        ClosingReportPeriod period, DateOnly date, CancellationToken cancellationToken = default)
    {
        var (from, exclusiveTo) = ComputeRange(period, date);
        var inclusiveTo = exclusiveTo.AddMilliseconds(-1);

        var profitLoss = await _profitLossService.GetSummaryAsync(from, inclusiveTo, cancellationToken);

        var sales = await _unitOfWork.Repository<Sale>()
            .FindAsync(s => s.SaleDate >= from && s.SaleDate <= inclusiveTo, cancellationToken);
        var purchases = await _unitOfWork.Repository<Purchase>()
            .FindAsync(p => p.PurchaseDate >= from && p.PurchaseDate <= inclusiveTo, cancellationToken);

        var cashIn = sales.Sum(s => s.PaidAmount);
        var cashOut = purchases.Sum(p => p.PaidAmount) + profitLoss.OperatingExpense;

        return new ClosingReportDto
        {
            PeriodType = period.ToString(),
            FromDate = from,
            ToDate = inclusiveTo,
            Revenue = profitLoss.Revenue,
            SalesCount = sales.Count,
            Cogs = profitLoss.Cogs,
            GrossProfit = profitLoss.GrossProfit,
            TotalPurchases = purchases.Sum(p => p.TotalAmount),
            PurchasesCount = purchases.Count,
            TotalExpenses = profitLoss.OperatingExpense,
            NetProfit = profitLoss.NetProfit,
            CashIn = cashIn,
            CashOut = cashOut,
            NetCashFlow = cashIn - cashOut
        };
    }

    private static (DateTimeOffset From, DateTimeOffset ExclusiveTo) ComputeRange(ClosingReportPeriod period, DateOnly date)
    {
        return period switch
        {
            ClosingReportPeriod.Day => DayRange(date),
            ClosingReportPeriod.Month => MonthRange(date),
            ClosingReportPeriod.Year => YearRange(date),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown closing report period.")
        };
    }

    private static (DateTimeOffset, DateTimeOffset) DayRange(DateOnly date)
    {
        var start = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddDays(1));
    }

    private static (DateTimeOffset, DateTimeOffset) MonthRange(DateOnly date)
    {
        var start = new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddMonths(1));
    }

    private static (DateTimeOffset, DateTimeOffset) YearRange(DateOnly date)
    {
        var start = new DateTimeOffset(date.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return (start, start.AddYears(1));
    }
}
