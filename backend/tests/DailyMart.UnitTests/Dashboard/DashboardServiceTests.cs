using System.Linq.Expressions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Dashboard;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Sales;
using DailyMart.Domain.Suppliers;
using Moq;

namespace DailyMart.UnitTests.Dashboard;

public class DashboardServiceTests
{
    private readonly Mock<IRepository<Sale>> _saleRepository = new();
    private readonly Mock<IRepository<SaleItem>> _saleItemRepository = new();
    private readonly Mock<IRepository<Purchase>> _purchaseRepository = new();
    private readonly Mock<IRepository<Customer>> _customerRepository = new();
    private readonly Mock<IRepository<Supplier>> _supplierRepository = new();
    private readonly Mock<IRepository<Product>> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly DashboardService _sut;

    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateTimeOffset TodayStart =
        new(Now.Year, Now.Month, Now.Day, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Yesterday = TodayStart.AddDays(-1).AddHours(10);
    private static readonly DateTimeOffset TwoMonthsAgo = TodayStart.AddDays(-60);

    public DashboardServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Sale>()).Returns(_saleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SaleItem>()).Returns(_saleItemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Purchase>()).Returns(_purchaseRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Customer>()).Returns(_customerRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Supplier>()).Returns(_supplierRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Product>()).Returns(_productRepository.Object);
        _sut = new DashboardService(_unitOfWork.Object);
    }

    private void SetUpSales(List<Sale> sales)
    {
        _saleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sales);
    }

    private void SetUpSaleItems(List<SaleItem> items)
    {
        _saleItemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SaleItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SaleItem, bool>> predicate, CancellationToken _) =>
                items.Where(predicate.Compile()).ToList());
    }

    private void SetUpPurchases(List<Purchase> purchases)
    {
        _purchaseRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(purchases);
    }

    private void SetUpCustomers(List<Customer> customers)
    {
        _customerRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers);
    }

    private void SetUpSuppliers(List<Supplier> suppliers)
    {
        _supplierRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(suppliers);
    }

    private void SetUpProducts(List<Product> products)
    {
        _productRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);
    }

    private void SetUpDefaults()
    {
        SetUpSales([]);
        SetUpSaleItems([]);
        SetUpPurchases([]);
        SetUpCustomers([]);
        SetUpSuppliers([]);
        SetUpProducts([]);
    }

    [Fact]
    public async Task GetSummaryAsync_only_counts_todays_sales_and_purchases_in_the_daily_figures()
    {
        SetUpDefaults();
        SetUpSales([
            new Sale { Id = 1, SaleDate = Now, TotalAmount = 100m, ProfitAmount = 30m, PaidAmount = 100m },
            new Sale { Id = 2, SaleDate = Yesterday, TotalAmount = 500m, ProfitAmount = 150m, PaidAmount = 500m }
        ]);
        SetUpPurchases([
            new Purchase { Id = 1, PurchaseDate = Now, TotalAmount = 60m, PaidAmount = 60m },
            new Purchase { Id = 2, PurchaseDate = Yesterday, TotalAmount = 200m, PaidAmount = 200m }
        ]);

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(100m, result.TodaySales);
        Assert.Equal(30m, result.TodayProfit);
        Assert.Equal(60m, result.TodayPurchases);
    }

    [Fact]
    public async Task GetSummaryAsync_computes_cash_in_hand_from_all_time_paid_amounts()
    {
        SetUpDefaults();
        SetUpSales([
            new Sale { Id = 1, SaleDate = Now, TotalAmount = 100m, PaidAmount = 100m },
            new Sale { Id = 2, SaleDate = Yesterday, TotalAmount = 500m, PaidAmount = 300m }
        ]);
        SetUpPurchases([
            new Purchase { Id = 1, PurchaseDate = Yesterday, TotalAmount = 200m, PaidAmount = 150m }
        ]);

        var result = await _sut.GetSummaryAsync();

        // (100 + 300) paid in - 150 paid out - 0 expense = 250
        Assert.Equal(250m, result.CashInHand);
    }

    [Fact]
    public async Task GetSummaryAsync_sums_customer_and_supplier_due_across_all_records()
    {
        SetUpDefaults();
        SetUpCustomers([
            new Customer { Id = 1, Name = "A", CurrentDue = 100m },
            new Customer { Id = 2, Name = "B", CurrentDue = 50m }
        ]);
        SetUpSuppliers([
            new Supplier { Id = 1, Name = "S1", CurrentDue = 400m }
        ]);

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(150m, result.TotalCustomerDue);
        Assert.Equal(400m, result.TotalSupplierDue);
    }

    [Fact]
    public async Task GetSummaryAsync_values_inventory_at_cost_and_flags_low_stock_correctly()
    {
        SetUpDefaults();
        SetUpProducts([
            new Product { Id = 1, Name = "Rice", CurrentStock = 10m, MinimumStock = 5m, PurchasePrice = 50m },
            new Product { Id = 2, Name = "Oil", CurrentStock = 2m, MinimumStock = 5m, PurchasePrice = 100m },
            new Product { Id = 3, Name = "Salt", CurrentStock = 1m, MinimumStock = 3m, PurchasePrice = 20m }
        ]);

        var result = await _sut.GetSummaryAsync();

        // (10*50) + (2*100) + (1*20) = 720
        Assert.Equal(720m, result.InventoryValue);
        Assert.Equal(2, result.LowStockCount);
        Assert.Equal(2, result.LowStockProducts.Count);
        // Lowest current stock first.
        Assert.Equal("Salt", result.LowStockProducts[0].ProductName);
        Assert.Equal("Oil", result.LowStockProducts[1].ProductName);
    }

    [Fact]
    public async Task GetSummaryAsync_ranks_top_selling_products_by_quantity_within_the_last_30_days()
    {
        SetUpDefaults();
        SetUpProducts([
            new Product { Id = 1, Name = "Rice" },
            new Product { Id = 2, Name = "Oil" }
        ]);
        SetUpSales([
            new Sale { Id = 1, SaleDate = Now },
            new Sale { Id = 2, SaleDate = TwoMonthsAgo }
        ]);
        SetUpSaleItems([
            new SaleItem { Id = 1, SaleId = 1, ProductId = 1, Quantity = 5m, LineTotal = 500m },
            new SaleItem { Id = 2, SaleId = 1, ProductId = 2, Quantity = 2m, LineTotal = 400m },
            // Belongs to a sale from two months ago - must be excluded from the 30-day ranking.
            new SaleItem { Id = 3, SaleId = 2, ProductId = 2, Quantity = 50m, LineTotal = 5000m }
        ]);

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(2, result.TopSellingProducts.Count);
        Assert.Equal("Rice", result.TopSellingProducts[0].ProductName);
        Assert.Equal(5m, result.TopSellingProducts[0].QuantitySold);
        Assert.Equal("Oil", result.TopSellingProducts[1].ProductName);
        Assert.Equal(2m, result.TopSellingProducts[1].QuantitySold);
        Assert.Equal(400m, result.TopSellingProducts[1].Revenue);
    }

    [Fact]
    public async Task GetSummaryAsync_returns_exactly_seven_trend_points_ending_today()
    {
        SetUpDefaults();

        var result = await _sut.GetSummaryAsync();

        Assert.Equal(7, result.SalesTrend.Count);
        Assert.Equal(DateOnly.FromDateTime(Now.UtcDateTime), result.SalesTrend[^1].Date);
    }
}
