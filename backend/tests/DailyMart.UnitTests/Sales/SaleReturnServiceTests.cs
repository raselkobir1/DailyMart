using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Customers;
using DailyMart.Application.Inventory;
using DailyMart.Application.Sales;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Sales;
using Moq;

namespace DailyMart.UnitTests.Sales;

public class SaleReturnServiceTests
{
    private readonly Mock<IRepository<Sale>> _saleRepository = new();
    private readonly Mock<IRepository<SaleItem>> _saleItemRepository = new();
    private readonly Mock<IRepository<SaleReturn>> _returnRepository = new();
    private readonly Mock<IRepository<SaleReturnItem>> _returnItemRepository = new();
    private readonly Mock<IRepository<Product>> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly Mock<ICustomerService> _customerService = new();
    private readonly SaleReturnService _sut;

    public SaleReturnServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Sale>()).Returns(_saleRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SaleItem>()).Returns(_saleItemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SaleReturn>()).Returns(_returnRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SaleReturnItem>()).Returns(_returnItemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Product>()).Returns(_productRepository.Object);

        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = 1 });

        _saleItemRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaleItem { Id = 1, SaleId = 1, ProductId = 1, Quantity = 5, UnitPrice = 50 });
        _saleItemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SaleItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SaleItem { Id = 1, SaleId = 1, ProductId = 1, Quantity = 5, UnitPrice = 50 }]);

        _returnItemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SaleReturnItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice 5kg" }]);

        _returnRepository
            .Setup(r => r.AddAsync(It.IsAny<SaleReturn>(), It.IsAny<CancellationToken>()))
            .Callback<SaleReturn, CancellationToken>((r, _) => r.Id = 20)
            .Returns(Task.CompletedTask);

        _sut = new SaleReturnService(_unitOfWork.Object, _inventoryService.Object, _customerService.Object);
    }

    private static SaleReturnRequestDto ValidRequest(decimal quantity = 2) => new()
    {
        ReturnDate = DateTimeOffset.UtcNow,
        Items = [new SaleReturnItemRequestDto { SaleItemId = 1, Quantity = quantity }]
    };

    [Fact]
    public async Task CreateAsync_throws_NotFoundException_when_the_sale_does_not_exist()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Sale?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(404, ValidRequest()));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_sale_item_does_not_belong_to_the_sale()
    {
        _saleItemRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaleItem { Id = 1, SaleId = 999, ProductId = 1, Quantity = 5, UnitPrice = 50 });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, ValidRequest()));
    }

    [Fact]
    public async Task CreateAsync_computes_UnitPrice_and_LineTotal_from_the_original_sale_line()
    {
        var result = await _sut.CreateAsync(1, ValidRequest(quantity: 2));

        Assert.Equal(100, result.TotalAmount); // 2 * 50
        Assert.Equal(50, result.Items[0].UnitPrice);
        Assert.Equal(100, result.Items[0].LineTotal);
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_quantity_exceeds_what_remains_returnable()
    {
        _returnItemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SaleReturnItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SaleReturnItem { Id = 1, SaleReturnId = 5, SaleItemId = 1, Quantity = 3, UnitPrice = 50, LineTotal = 150 }]);

        // Original line has quantity 5; 3 already returned -> only 2 remains returnable.
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, ValidRequest(quantity: 3)));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_quantity_is_zero()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, ValidRequest(quantity: 0)));
    }

    [Fact]
    public async Task CreateAsync_posts_a_positive_inventory_transaction_and_decreases_the_customer_due()
    {
        await _sut.CreateAsync(1, ValidRequest(quantity: 2));

        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.SaleReturn, 2, nameof(SaleReturn), 20, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _customerService.Verify(s => s.AdjustDueAsync(
            1, -100, CustomerLedgerEntryType.SaleReturn, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_never_calls_AdjustDueAsync_when_the_original_sale_had_no_customer()
    {
        _saleRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sale { Id = 1, CustomerId = null });

        await _sut.CreateAsync(1, ValidRequest(quantity: 2));

        _customerService.Verify(s => s.AdjustDueAsync(
            It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<CustomerLedgerEntryType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPagedAsync_throws_NotFoundException_when_the_sale_does_not_exist()
    {
        _saleRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Sale, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetPagedAsync(404, new PagedRequest()));
    }
}
