using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Expenses;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Sales;
using DailyMart.Domain.Suppliers;

namespace DailyMart.Application.Dashboard;

/// <summary>
/// Reads across Sale/Purchase/Customer/Supplier/Product/Expense - via <see cref="IRepository{T}.FindAsync"/>/
/// GetAllAsync, never <c>Query()</c> - so this stays a plain LINQ-to-objects aggregation with no direct
/// EF Core dependency in the Application layer (Query()'s IQueryable would need Microsoft.EntityFrameworkCore
/// async operators like SumAsync/ToListAsync, which only Infrastructure references).
/// </summary>
public class DashboardService : IDashboardService
{
    private const int TopSellingProductCount = 5;
    private const int LowStockProductCount = 5;
    private const int TopSellingLookbackDays = 30;
    private const int TrendDays = 7;

    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);
        var trendStart = todayStart.AddDays(-(TrendDays - 1));

        var allSales = await _unitOfWork.Repository<Sale>().GetAllAsync(cancellationToken);
        var allPurchases = await _unitOfWork.Repository<Purchase>().GetAllAsync(cancellationToken);
        var sales = allSales.Where(s => s.SaleDate >= trendStart).ToList();
        var purchases = allPurchases.Where(p => p.PurchaseDate >= trendStart).ToList();

        var todaySales = sales.Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd).Sum(s => s.TotalAmount);
        var todayProfit = sales.Where(s => s.SaleDate >= todayStart && s.SaleDate < todayEnd).Sum(s => s.ProfitAmount);
        var todayPurchases = purchases
            .Where(p => p.PurchaseDate >= todayStart && p.PurchaseDate < todayEnd)
            .Sum(p => p.TotalAmount);

        var allExpenses = await _unitOfWork.Repository<Expense>().GetAllAsync(cancellationToken);
        var todayExpense = allExpenses
            .Where(e => e.ExpenseDate >= todayStart && e.ExpenseDate < todayEnd)
            .Sum(e => e.Amount);
        var cashInHand = allSales.Sum(s => s.PaidAmount) - allPurchases.Sum(p => p.PaidAmount) - allExpenses.Sum(e => e.Amount);

        var customers = await _unitOfWork.Repository<Customer>().GetAllAsync(cancellationToken);
        var suppliers = await _unitOfWork.Repository<Supplier>().GetAllAsync(cancellationToken);
        var totalCustomerDue = customers.Sum(c => c.CurrentDue);
        var totalSupplierDue = suppliers.Sum(s => s.CurrentDue);

        var products = await _unitOfWork.Repository<Product>().GetAllAsync(cancellationToken);
        var inventoryValue = products.Sum(p => p.CurrentStock * p.PurchasePrice);

        var lowStockAll = products.Where(p => p.CurrentStock <= p.MinimumStock).ToList();
        var lowStockProducts = lowStockAll
            .OrderBy(p => p.CurrentStock)
            .Take(LowStockProductCount)
            .Select(p => new LowStockProductDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock
            })
            .ToList();

        var topSellingSince = todayStart.AddDays(-(TopSellingLookbackDays - 1));
        var recentSaleIds = allSales
            .Where(s => s.SaleDate >= topSellingSince)
            .Select(s => s.Id)
            .ToHashSet();
        var recentSaleItems = await _unitOfWork.Repository<SaleItem>()
            .FindAsync(item => recentSaleIds.Contains(item.SaleId), cancellationToken);
        var productNames = products.ToDictionary(p => p.Id, p => p.Name);

        var topSelling = recentSaleItems
            .GroupBy(item => item.ProductId)
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key,
                ProductName = productNames.GetValueOrDefault(g.Key, "(deleted product)"),
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(TopSellingProductCount)
            .ToList();

        var trend = new List<DashboardTrendPointDto>();
        for (var i = 0; i < TrendDays; i++)
        {
            var day = DateOnly.FromDateTime(trendStart.AddDays(i).UtcDateTime);
            var daySales = sales.Where(s => DateOnly.FromDateTime(s.SaleDate.UtcDateTime) == day).ToList();
            var dayPurchases = purchases.Where(p => DateOnly.FromDateTime(p.PurchaseDate.UtcDateTime) == day).ToList();

            trend.Add(new DashboardTrendPointDto
            {
                Date = day,
                Sales = daySales.Sum(s => s.TotalAmount),
                Purchases = dayPurchases.Sum(p => p.TotalAmount),
                Profit = daySales.Sum(s => s.ProfitAmount)
            });
        }

        return new DashboardSummaryDto
        {
            TodaySales = todaySales,
            TodayPurchases = todayPurchases,
            TodayProfit = todayProfit,
            TodayExpense = todayExpense,
            CashInHand = cashInHand,
            TotalCustomerDue = totalCustomerDue,
            TotalSupplierDue = totalSupplierDue,
            InventoryValue = inventoryValue,
            LowStockCount = lowStockAll.Count,
            LowStockProducts = lowStockProducts,
            TopSellingProducts = topSelling,
            SalesTrend = trend
        };
    }
}
