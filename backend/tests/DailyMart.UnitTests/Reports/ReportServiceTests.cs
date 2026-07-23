using System.Linq.Expressions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.ProfitLoss;
using DailyMart.Application.Reports;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Sales;
using Moq;

namespace DailyMart.UnitTests.Reports;

public class ReportServiceTests
{
    private readonly Mock<IRepository<Sale>> _saleRepository = new();
    private readonly Mock<IRepository<Purchase>> _purchaseRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IProfitLossService> _profitLossService = new();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Sale>()).Returns(_saleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Purchase>()).Returns(_purchaseRepository.Object);
        _sut = new ReportService(_unitOfWork.Object, _profitLossService.Object);
    }

    private void SetUpSales(List<Sale> sales)
    {
        _saleRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Sale, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Sale, bool>> predicate, CancellationToken _) => sales.Where(predicate.Compile()).ToList());
    }

    private void SetUpPurchases(List<Purchase> purchases)
    {
        _purchaseRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Purchase, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Purchase, bool>> predicate, CancellationToken _) => purchases.Where(predicate.Compile()).ToList());
    }

    private void SetUpProfitLoss(decimal revenue, decimal cogs, decimal operatingExpense)
    {
        _profitLossService
            .Setup(s => s.GetSummaryAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfitLossSummaryDto
            {
                Revenue = revenue,
                Cogs = cogs,
                GrossProfit = revenue - cogs,
                OperatingExpense = operatingExpense,
                NetProfit = revenue - cogs - operatingExpense
            });
    }

    [Fact]
    public async Task GetClosingReportAsync_Day_covers_exactly_the_requested_calendar_day()
    {
        SetUpSales([]);
        SetUpPurchases([]);
        SetUpProfitLoss(0, 0, 0);

        DateTimeOffset? capturedFrom = null;
        DateTimeOffset? capturedTo = null;
        _profitLossService
            .Setup(s => s.GetSummaryAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Callback<DateTimeOffset, DateTimeOffset, CancellationToken>((from, to, _) => { capturedFrom = from; capturedTo = to; })
            .ReturnsAsync(new ProfitLossSummaryDto());

        var result = await _sut.GetClosingReportAsync(ClosingReportPeriod.Day, new DateOnly(2026, 7, 23));

        Assert.Equal(new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero), result.FromDate);
        Assert.Equal(new DateTimeOffset(2026, 7, 23, 23, 59, 59, 999, TimeSpan.Zero), result.ToDate);
        Assert.Equal(result.FromDate, capturedFrom);
        Assert.Equal(result.ToDate, capturedTo);
        Assert.Equal("Day", result.PeriodType);
    }

    [Fact]
    public async Task GetClosingReportAsync_Month_covers_the_whole_calendar_month_regardless_of_the_given_day()
    {
        SetUpSales([]);
        SetUpPurchases([]);
        SetUpProfitLoss(0, 0, 0);

        var result = await _sut.GetClosingReportAsync(ClosingReportPeriod.Month, new DateOnly(2026, 7, 15));

        Assert.Equal(new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero), result.FromDate);
        Assert.Equal(new DateTimeOffset(2026, 7, 31, 23, 59, 59, 999, TimeSpan.Zero), result.ToDate);
    }

    [Fact]
    public async Task GetClosingReportAsync_Year_covers_the_whole_calendar_year()
    {
        SetUpSales([]);
        SetUpPurchases([]);
        SetUpProfitLoss(0, 0, 0);

        var result = await _sut.GetClosingReportAsync(ClosingReportPeriod.Year, new DateOnly(2026, 3, 1));

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), result.FromDate);
        Assert.Equal(new DateTimeOffset(2026, 12, 31, 23, 59, 59, 999, TimeSpan.Zero), result.ToDate);
    }

    [Fact]
    public async Task GetClosingReportAsync_computes_purchases_and_cash_flow_from_the_period()
    {
        var day = new DateOnly(2026, 7, 23);
        var withinDay = new DateTimeOffset(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);
        var outsideDay = new DateTimeOffset(2026, 7, 24, 0, 0, 1, TimeSpan.Zero);

        SetUpSales([
            new Sale { Id = 1, SaleDate = withinDay, PaidAmount = 500m },
            new Sale { Id = 2, SaleDate = outsideDay, PaidAmount = 9999m }
        ]);
        SetUpPurchases([
            new Purchase { Id = 1, PurchaseDate = withinDay, TotalAmount = 300m, PaidAmount = 200m },
            new Purchase { Id = 2, PurchaseDate = outsideDay, TotalAmount = 9999m, PaidAmount = 9999m }
        ]);
        SetUpProfitLoss(revenue: 500m, cogs: 300m, operatingExpense: 100m);

        var result = await _sut.GetClosingReportAsync(ClosingReportPeriod.Day, day);

        Assert.Equal(1, result.SalesCount);
        Assert.Equal(300m, result.TotalPurchases);
        Assert.Equal(1, result.PurchasesCount);
        // CashIn = Sale.PaidAmount (500), CashOut = Purchase.PaidAmount (200) + OperatingExpense (100) = 300.
        Assert.Equal(500m, result.CashIn);
        Assert.Equal(300m, result.CashOut);
        Assert.Equal(200m, result.NetCashFlow);
        Assert.Equal(500m, result.Revenue);
        Assert.Equal(300m, result.Cogs);
        Assert.Equal(200m, result.GrossProfit);
        Assert.Equal(100m, result.TotalExpenses);
        Assert.Equal(100m, result.NetProfit);
    }
}
