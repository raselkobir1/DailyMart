using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Inventory;
using DailyMart.Application.Purchases;
using DailyMart.Application.Suppliers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Suppliers;
using Moq;

namespace DailyMart.UnitTests.Purchases;

public class PurchaseReturnServiceTests
{
    private readonly Mock<IRepository<Purchase>> _purchaseRepository = new();
    private readonly Mock<IRepository<PurchaseItem>> _purchaseItemRepository = new();
    private readonly Mock<IRepository<PurchaseReturn>> _returnRepository = new();
    private readonly Mock<IRepository<PurchaseReturnItem>> _returnItemRepository = new();
    private readonly Mock<IRepository<Product>> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly Mock<ISupplierService> _supplierService = new();
    private readonly PurchaseReturnService _sut;

    public PurchaseReturnServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Purchase>()).Returns(_purchaseRepository.Object);
        _unitOfWork.Setup(u => u.Repository<PurchaseItem>()).Returns(_purchaseItemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<PurchaseReturn>()).Returns(_returnRepository.Object);
        _unitOfWork.Setup(u => u.Repository<PurchaseReturnItem>()).Returns(_returnItemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Product>()).Returns(_productRepository.Object);

        _purchaseRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Purchase { Id = 1, SupplierId = 1 });

        _purchaseItemRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseItem { Id = 1, PurchaseId = 1, ProductId = 1, Quantity = 5, UnitPrice = 50 });
        _purchaseItemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PurchaseItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PurchaseItem { Id = 1, PurchaseId = 1, ProductId = 1, Quantity = 5, UnitPrice = 50 }]);

        _returnItemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PurchaseReturnItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice 5kg" }]);

        _returnRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseReturn>(), It.IsAny<CancellationToken>()))
            .Callback<PurchaseReturn, CancellationToken>((r, _) => r.Id = 20)
            .Returns(Task.CompletedTask);

        _sut = new PurchaseReturnService(_unitOfWork.Object, _inventoryService.Object, _supplierService.Object);
    }

    private static PurchaseReturnRequestDto ValidRequest(decimal quantity = 2) => new()
    {
        ReturnDate = DateTimeOffset.UtcNow,
        Items = [new PurchaseReturnItemRequestDto { PurchaseItemId = 1, Quantity = quantity }]
    };

    [Fact]
    public async Task CreateAsync_throws_NotFoundException_when_the_purchase_does_not_exist()
    {
        _purchaseRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Purchase?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.CreateAsync(404, ValidRequest()));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_purchase_item_does_not_belong_to_the_purchase()
    {
        _purchaseItemRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseItem { Id = 1, PurchaseId = 999, ProductId = 1, Quantity = 5, UnitPrice = 50 });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, ValidRequest()));
    }

    [Fact]
    public async Task CreateAsync_computes_UnitPrice_and_LineTotal_from_the_original_purchase_line()
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
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PurchaseReturnItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PurchaseReturnItem { Id = 1, PurchaseReturnId = 5, PurchaseItemId = 1, Quantity = 3, UnitPrice = 50, LineTotal = 150 }]);

        // Original line has quantity 5; 3 already returned -> only 2 remains returnable.
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, ValidRequest(quantity: 3)));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_quantity_is_zero()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(1, ValidRequest(quantity: 0)));
    }

    [Fact]
    public async Task CreateAsync_posts_a_negative_inventory_transaction_and_decreases_the_supplier_due()
    {
        await _sut.CreateAsync(1, ValidRequest(quantity: 2));

        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.PurchaseReturn, -2, nameof(PurchaseReturn), 20, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _supplierService.Verify(s => s.AdjustDueAsync(
            1, -100, SupplierLedgerEntryType.PurchaseReturn, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedAsync_throws_NotFoundException_when_the_purchase_does_not_exist()
    {
        _purchaseRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Purchase, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetPagedAsync(404, new PagedRequest()));
    }
}
