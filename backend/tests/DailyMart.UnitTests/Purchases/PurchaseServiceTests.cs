using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Inventory;
using DailyMart.Application.Purchases;
using DailyMart.Application.Suppliers;
using DailyMart.Domain.Common;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Suppliers;
using Moq;

namespace DailyMart.UnitTests.Purchases;

public class PurchaseServiceTests
{
    private readonly Mock<IRepository<Purchase>> _purchaseRepository = new();
    private readonly Mock<IRepository<PurchaseItem>> _itemRepository = new();
    private readonly Mock<IRepository<Supplier>> _supplierRepository = new();
    private readonly Mock<IRepository<Product>> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IInventoryService> _inventoryService = new();
    private readonly Mock<ISupplierService> _supplierService = new();
    private readonly PurchaseService _sut;

    public PurchaseServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Purchase>()).Returns(_purchaseRepository.Object);
        _unitOfWork.Setup(u => u.Repository<PurchaseItem>()).Returns(_itemRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Supplier>()).Returns(_supplierRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Product>()).Returns(_productRepository.Object);

        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _supplierRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Supplier { Id = 1, Name = "Acme Distributors" }]);

        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice 5kg", Code = "P001" }]);

        _purchaseRepository
            .Setup(r => r.AddAsync(It.IsAny<Purchase>(), It.IsAny<CancellationToken>()))
            .Callback<Purchase, CancellationToken>((p, _) => p.Id = 10)
            .Returns(Task.CompletedTask);

        _sut = new PurchaseService(_unitOfWork.Object, _inventoryService.Object, _supplierService.Object);
    }

    private static PurchaseRequestDto ValidRequest(
        PaymentType paymentType = PaymentType.Cash,
        decimal paidAmount = 0,
        decimal headerDiscount = 0,
        decimal headerVat = 0,
        decimal itemQuantity = 2,
        decimal itemUnitPrice = 50,
        decimal itemDiscount = 0) => new()
    {
        SupplierId = 1,
        PurchaseDate = DateTimeOffset.UtcNow,
        PaymentType = paymentType,
        DiscountAmount = headerDiscount,
        VatAmount = headerVat,
        PaidAmount = paidAmount,
        Items = [new PurchaseItemRequestDto { ProductId = 1, Quantity = itemQuantity, UnitPrice = itemUnitPrice, DiscountAmount = itemDiscount }]
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
    public async Task CreateAsync_Credit_sets_PaidAmount_to_zero_and_DueAmount_to_the_total()
    {
        var result = await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Credit));

        Assert.Equal(100, result.TotalAmount);
        Assert.Equal(0, result.PaidAmount);
        Assert.Equal(100, result.DueAmount);
    }

    [Fact]
    public async Task CreateAsync_Partial_uses_the_callers_PaidAmount_when_it_is_valid()
    {
        var result = await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, paidAmount: 40));

        Assert.Equal(40, result.PaidAmount);
        Assert.Equal(60, result.DueAmount);
    }

    [Fact]
    public async Task CreateAsync_Partial_throws_when_PaidAmount_is_zero()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, paidAmount: 0)));
    }

    [Fact]
    public async Task CreateAsync_Partial_throws_when_PaidAmount_is_greater_than_or_equal_to_the_total()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Partial, paidAmount: 100)));
    }

    [Fact]
    public async Task CreateAsync_throws_BusinessRuleException_when_the_supplier_does_not_exist()
    {
        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidRequest()));
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
    public async Task CreateAsync_posts_one_inventory_transaction_per_item_with_the_Purchase_type_and_a_positive_quantity()
    {
        var request = new PurchaseRequestDto
        {
            SupplierId = 1,
            PurchaseDate = DateTimeOffset.UtcNow,
            PaymentType = PaymentType.Cash,
            Items =
            [
                new PurchaseItemRequestDto { ProductId = 1, Quantity = 2, UnitPrice = 50, DiscountAmount = 0 },
                new PurchaseItemRequestDto { ProductId = 2, Quantity = 3, UnitPrice = 20, DiscountAmount = 0 }
            ]
        };
        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice" }, new Product { Id = 2, Name = "Oil" }]);

        await _sut.CreateAsync(request);

        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.Purchase, 2, nameof(Purchase), 10, null, It.IsAny<CancellationToken>()), Times.Once);
        _inventoryService.Verify(s => s.RecordTransactionAsync(
            2, InventoryTransactionType.Purchase, 3, nameof(Purchase), 10, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_calls_AdjustDueAsync_when_a_due_amount_is_created()
    {
        await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Credit));

        _supplierService.Verify(s => s.AdjustDueAsync(
            1, 100, SupplierLedgerEntryType.Purchase, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_never_calls_AdjustDueAsync_for_a_fully_paid_Cash_purchase()
    {
        await _sut.CreateAsync(ValidRequest(paymentType: PaymentType.Cash));

        _supplierService.Verify(s => s.AdjustDueAsync(
            It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<SupplierLedgerEntryType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _purchaseRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Purchase?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(404));
    }

    [Fact]
    public async Task GetByIdAsync_maps_supplier_and_product_names_via_lookups()
    {
        var purchase = new Purchase { Id = 10, SupplierId = 1, PaymentType = PaymentType.Cash };
        var items = new List<PurchaseItem>
        {
            new() { Id = 1, PurchaseId = 10, ProductId = 1, Quantity = 2, UnitPrice = 50, LineTotal = 100 }
        };

        _purchaseRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(purchase);
        _itemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PurchaseItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var result = await _sut.GetByIdAsync(10);

        Assert.Equal("PUR-000010", result.PurchaseNumber);
        Assert.Equal("Acme Distributors", result.SupplierName);
        Assert.Equal("Rice 5kg", result.Items[0].ProductName);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_missing()
    {
        _purchaseRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Purchase?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(404, ValidRequest()));
    }

    [Fact]
    public async Task UpdateAsync_reverses_the_old_items_and_due_then_reapplies_the_new_request()
    {
        var existing = new Purchase { Id = 10, SupplierId = 1, PaymentType = PaymentType.Credit, TotalAmount = 100, DueAmount = 100 };
        var oldItems = new List<PurchaseItem>
        {
            new() { Id = 1, PurchaseId = 10, ProductId = 1, Quantity = 2, UnitPrice = 50, LineTotal = 100 }
        };

        _purchaseRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _itemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PurchaseItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldItems);

        // Cash this time - the new request creates no due.
        var result = await _sut.UpdateAsync(10, ValidRequest(paymentType: PaymentType.Cash));

        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.Adjustment, -2, nameof(Purchase), 10, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.Purchase, 2, nameof(Purchase), 10, null, It.IsAny<CancellationToken>()), Times.Once);

        _supplierService.Verify(s => s.AdjustDueAsync(
            1, -100, SupplierLedgerEntryType.Adjustment, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        // Cash creates no new due, so the reversal above is the only AdjustDueAsync call in this whole update.
        _supplierService.Verify(s => s.AdjustDueAsync(
            It.IsAny<long>(), It.IsAny<decimal>(), It.IsAny<SupplierLedgerEntryType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _itemRepository.Verify(r => r.Remove(oldItems[0]), Times.Once);
        Assert.Equal(0, result.DueAmount);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_missing()
    {
        _purchaseRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Purchase?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }

    [Fact]
    public async Task DeleteAsync_reverses_stock_and_due_effects_then_removes_the_purchase_and_its_items()
    {
        var existing = new Purchase { Id = 10, SupplierId = 1, DueAmount = 100 };
        var items = new List<PurchaseItem>
        {
            new() { Id = 1, PurchaseId = 10, ProductId = 1, Quantity = 2, UnitPrice = 50, LineTotal = 100 }
        };

        _purchaseRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _itemRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PurchaseItem, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        await _sut.DeleteAsync(10);

        _inventoryService.Verify(s => s.RecordTransactionAsync(
            1, InventoryTransactionType.Adjustment, -2, nameof(Purchase), 10, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _supplierService.Verify(s => s.AdjustDueAsync(
            1, -100, SupplierLedgerEntryType.Adjustment, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        _itemRepository.Verify(r => r.Remove(items[0]), Times.Once);
        _purchaseRepository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
