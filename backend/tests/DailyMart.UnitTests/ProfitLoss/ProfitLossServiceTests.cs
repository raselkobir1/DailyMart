using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.ProfitLoss;
using DailyMart.Domain.Expenses;
using DailyMart.Domain.Sales;
using Moq;

namespace DailyMart.UnitTests.ProfitLoss;

public class ProfitLossServiceTests
{
    private readonly Mock<IRepository<Sale>> _saleRepository = new();
    private readonly Mock<IRepository<Expense>> _expenseRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly ProfitLossService _sut;

    private static readonly DateTimeOffset PeriodStart = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PeriodEnd = new(2026, 7, 31, 23, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset OutsidePeriod = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);

    public ProfitLossServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Sale>()).Returns(_saleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Expense>()).Returns(_expenseRepository.Object);
        _sut = new ProfitLossService(_unitOfWork.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_computes_revenue_cogs_gross_and_net_profit_correctly()
    {
        _saleRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sale, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Sale, bool>> predicate, CancellationToken _) =>
                new List<Sale>
                {
                    new() { Id = 1, SaleDate = PeriodStart.AddDays(5), TotalAmount = 1000m, TotalCost = 600m },
                    new() { Id = 2, SaleDate = PeriodStart.AddDays(10), TotalAmount = 500m, TotalCost = 350m },
                    // Outside the requested period - must be excluded.
                    new() { Id = 3, SaleDate = OutsidePeriod, TotalAmount = 9999m, TotalCost = 1m }
                }.Where(predicate.Compile()).ToList());

        _expenseRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Expense, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Expense, bool>> predicate, CancellationToken _) =>
                new List<Expense>
                {
                    new() { Id = 1, ExpenseDate = PeriodStart.AddDays(3), Amount = 400m, Category = ExpenseCategory.Rent },
                    // Outside the requested period - must be excluded.
                    new() { Id = 2, ExpenseDate = OutsidePeriod, Amount = 8888m, Category = ExpenseCategory.Salary }
                }.Where(predicate.Compile()).ToList());

        var result = await _sut.GetSummaryAsync(PeriodStart, PeriodEnd);

        Assert.Equal(1500m, result.Revenue);
        Assert.Equal(950m, result.Cogs);
        Assert.Equal(550m, result.GrossProfit);
        Assert.Equal(400m, result.OperatingExpense);
        Assert.Equal(150m, result.NetProfit);
    }

    [Fact]
    public async Task GetSummaryAsync_throws_BusinessRuleException_when_fromDate_is_after_toDate()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.GetSummaryAsync(PeriodEnd, PeriodStart));
    }

    [Fact]
    public async Task GetSummaryAsync_returns_all_zeros_when_no_sales_or_expenses_in_range()
    {
        _saleRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Sale, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _expenseRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Expense, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetSummaryAsync(PeriodStart, PeriodEnd);

        Assert.Equal(0m, result.Revenue);
        Assert.Equal(0m, result.Cogs);
        Assert.Equal(0m, result.GrossProfit);
        Assert.Equal(0m, result.OperatingExpense);
        Assert.Equal(0m, result.NetProfit);
    }
}
