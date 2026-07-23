using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Expenses;
using DailyMart.Domain.Sales;

namespace DailyMart.Application.ProfitLoss;

public class ProfitLossService : IProfitLossService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProfitLossService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProfitLossSummaryDto> GetSummaryAsync(
        DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken = default)
    {
        if (fromDate > toDate)
        {
            throw new BusinessRuleException("'From' date must not be after 'to' date.");
        }

        var sales = await _unitOfWork.Repository<Sale>()
            .FindAsync(s => s.SaleDate >= fromDate && s.SaleDate <= toDate, cancellationToken);
        var expenses = await _unitOfWork.Repository<Expense>()
            .FindAsync(e => e.ExpenseDate >= fromDate && e.ExpenseDate <= toDate, cancellationToken);

        var revenue = sales.Sum(s => s.TotalAmount);
        var cogs = sales.Sum(s => s.TotalCost);
        var grossProfit = revenue - cogs;
        var operatingExpense = expenses.Sum(e => e.Amount);
        var netProfit = grossProfit - operatingExpense;

        return new ProfitLossSummaryDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Revenue = revenue,
            Cogs = cogs,
            GrossProfit = grossProfit,
            OperatingExpense = operatingExpense,
            NetProfit = netProfit
        };
    }
}
