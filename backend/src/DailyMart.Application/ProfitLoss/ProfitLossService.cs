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

        // A return is netted into the period it happened in (by ReturnDate), not retroactively rewriting
        // the original sale's period - standard accounting treats a return as a contra-revenue event in
        // the period it occurs, rather than reopening an already-reported prior period. Without this,
        // revenue/COGS/profit for any period stayed exactly what it was at the moment of sale forever,
        // even after goods were returned and the sale effectively partially undone.
        var (returnedRevenue, returnedCogs) = await GetReturnedAmountsAsync(fromDate, toDate, cancellationToken);

        var revenue = sales.Sum(s => s.TotalAmount) - returnedRevenue;
        var cogs = sales.Sum(s => s.TotalCost) - returnedCogs;
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

    /// <summary>Revenue reduction is each return's own TotalAmount (already the sum of returned lines'
    /// Quantity * original UnitPrice). COGS reduction re-joins each returned line back to its original
    /// SaleItem for the UnitCost snapshotted at sale time - SaleReturnItem itself only stores UnitPrice,
    /// not cost.</summary>
    private async Task<(decimal ReturnedRevenue, decimal ReturnedCogs)> GetReturnedAmountsAsync(
        DateTimeOffset fromDate, DateTimeOffset toDate, CancellationToken cancellationToken)
    {
        var saleReturns = await _unitOfWork.Repository<SaleReturn>()
            .FindAsync(r => r.ReturnDate >= fromDate && r.ReturnDate <= toDate, cancellationToken);

        if (saleReturns.Count == 0)
        {
            return (0m, 0m);
        }

        var returnedRevenue = saleReturns.Sum(r => r.TotalAmount);

        var returnIds = saleReturns.Select(r => r.Id).ToList();
        var returnItems = await _unitOfWork.Repository<SaleReturnItem>()
            .FindAsync(i => returnIds.Contains(i.SaleReturnId), cancellationToken);

        var saleItemIds = returnItems.Select(i => i.SaleItemId).Distinct().ToList();
        var unitCostBySaleItemId = (await _unitOfWork.Repository<SaleItem>()
            .FindAsync(i => saleItemIds.Contains(i.Id), cancellationToken))
            .ToDictionary(i => i.Id, i => i.UnitCost);

        var returnedCogs = returnItems.Sum(i => i.Quantity * unitCostBySaleItemId.GetValueOrDefault(i.SaleItemId));

        return (returnedRevenue, returnedCogs);
    }
}
