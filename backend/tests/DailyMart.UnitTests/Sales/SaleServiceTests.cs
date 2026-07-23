using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Customers;
using DailyMart.Application.Inventory;
using DailyMart.Application.Sales;
using DailyMart.Domain.Common;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Sales;
using Moq;

namespace DailyMart.UnitTests.Sales;

public class SaleServiceTests
{
    private readonly Mock<IRepository<Sale>> _saleRepository = new();
    private readonly Mock<IRepository<SaleItem>> _itemRepository = new();
    private readonly Mock<IRepository<Customer>> _customerRepository = new();
    private readonly Mock<IRepository<Product>> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly Mock<ICustomerService> _customerService = new();
    private readonly SaleService _sut;

    public SaleServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Sale>()).Returns(_saleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SaleItem>()).Returns(_itemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Customer>()).Returns(_customerRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Product>()).Returns(_productRepository.Object);

        _customerRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _customerRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Customer { Id = 1, Name = "Jane Doe" }]);

        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice 5kg", Code = "P001", PurchasePrice = 30 }]);

        _saleRepository
            .Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Callback<Sale, CancellationToken>((s, _) => s.Id = 10)
            .Returns(Task.CompletedTask);

        _sut = new SaleService(_unitOfWork.Object, _inventoryService.Object, _customerService.Object);
    }

    private static SaleRequestDto ValidRequest(
        PaymentType paymentType = PaymentType.Cash,
        long? customerId = null,
        decimal paidAmount = 0,
        decimal headerDiscount = 0,
        decimal headerVat = 0,
        decimal itemQuantity = 2,
        decimal itemUnitPrice = 50,
        decimal itemDiscount = 0) => new()
    {
        CustomerId = customerId,
        SaleDate = DateTimeOffset.UtcNow,
        PaymentType = paymentType,
        DiscountAmount = headerDiscount,
        VatAmount = headerVat,
        PaidAmount = paidAmount,
        Items = [new SaleItemRequestDto { ProductId = 1, Quantity = itemQuantity, UnitPrice = itemUnitPrice, DiscountAmount = itemDiscount }]
    };

    [Fact]
    public async Task CreateAsync_computes_line_totals_and_amounts_for_Cash()
    {
        var request = ValidRequest(headerDiscount: 10, headerVat: 5, itemDiscount: 5);

        var result = await _sut.CreateAsync(request);

        Assert.Equal(95, result.SubtotalAmount); // (2 * 50) - 5
        Assert.Equal(90, result.TotalAmount); // 95 - 10 + 5
        Assert.Equal(90, result.PaidAmount);
        Assert.Equal(0, result.DueAmount);
        Assert.Equal(95, result.Items[0].LineTotal);
    }

    [Fact]
    public async Task CreateAsync_snapshots_UnitCost_from_the_products_PurchasePrice_and_computes_profit()
    {
        var result = await _sut.CreateAsync(ValidRequest(itemQuantity: 2, itemUnitPrice: 50));

        // UnitCost = 30 (Product.PurchasePrice), TotalCost = 2 * 30 = 60, TotalAmount = 100, Profit = 40.
        Assert.Equal(30, result.Items[0].UnitCost);
        Assert.Equal(60, result.TotalCost);
        Assert.Equal(40, result.ProfitAmount);
    }

    [Fact]
    public async Task CreateAsync_Credit_sets_PaidAmount_to_zero_and_DueAmount_to_the_total()
    {
        var result = await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Credit, customerId: 1));

        Assert.Equal(100, result.TotalAmount);
        Assert.Equal(0, result.PaidAmount);
        Assert.Equal(100, result.DueAmount);
    }

    [Fact]
    public async Task CreateAsync_Partial_uses_the_callers_PaidAmount_when_it_is_valid()
    {
        var result = await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, customerId: 1, paidAmount: 40));

        Assert.Equal(40, result.PaidAmount);
        Assert.Equal(60, result.DueAmount);
    }

    [Fact]
    public async Task CreateAsync_Partial_throws_when_PaidAmount_is_zero()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, customerId: 1, paidAmount: 0)));
    }

    [Fact]
    public async Task CreateAsync_Partial_throws_when_PaidAmount_is_greater_than_or_equal_to_the_total()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, customerId: 1, paidAmount: 100)));
    }

    [Fact]
    public async Task CreateAsync_Cash_sale_needs_no_customer()
    {
        var result = await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Cash, customerId: null));

        Assert.Null(result.CustomerId);
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_Credit_has_no_customer()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Credit, customerId: null)));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_Partial_has_no_customer()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, customerId: null, paidAmount: 40)));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_customer_does_not_exist()
    {
        _customerRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Credit, customerId: 1)));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_a_product_does_not_exist()
    {
        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidRequest()));
    }

    [Fact]
    public async Task CreateAsync_posts_one_negative_inventory_transaction_per_item_with_the_Sale_type()
    {
        var request = new SaleRequestDto
        {
            SaleDate = DateTimeOffset.UtcNow,
            PaymentType = PaymentType.Cash,
            Items =
            [
                new SaleItemRequestDto { ProductId = 1, Quantity = 2, UnitPrice = 50, DiscountAmount = 0 },
                new SaleItemRequestDto { ProductId = 2, Quantity = 3, UnitPrice = 20, DiscountAmount = 0 }
            ]
        };
        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice" }, new Product { Id = 2, Name = "Oil" }]);

        await _sut.CreateAsync(request);

        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.Sale, -2, nameof(Sale), 10, null, It.IsAny<CancellationToken>()), Times.Once);
        _inventoryService.Verify(s => s.RecordTransactionAsync(
            2, InventoryTransactionType.Sale, -3, nameof(Sale), 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_calls_AdjustDueAsync_when_a_credit_sale_creates_a_due()
    {
        await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Credit, customerId: 1));

        _customerService.Verify(s => s.AdjustDueAsync(
            1, 100, CustomerLedgerEntryType.Sale, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_never_calls_AdjustDueAsync_for_a_fully_paid_Cash_sale()
    {
        await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Cash));

        _customerService.Verify(s => s.AdjustDueAsync(
            It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<CustomerLedgerEntryType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Sale?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(404));
    }

    [Fact]
    public async Task GetByIdAsync_maps_customer_and_product_names_via_lookups()
    {
        var sale = new Sale { Id = 10, CustomerId = 1, PaymentType = PaymentType.Cash };
        var items = new List<SaleItem>
        {
            new() { Id = 1, SaleId = 10, ProductId = 1, Quantity = 2, UnitPrice = 50, LineTotal = 100 }
        };

        _saleRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(sale);
        _itemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SaleItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _sut.GetByIdAsync(10);

        Assert.Equal("SALE-000010", result.SaleNumber);
        Assert.Equal("Jane Doe", result.CustomerName);
        Assert.Equal("Rice 5kg", result.Items[0].ProductName);
    }
}
