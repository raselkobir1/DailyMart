using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Inventory;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using Moq;

namespace DailyMart.UnitTests.Inventory;

public class InventoryServiceTests
{
    private readonly Mock<IRepository<Product>> _productRepository = new();
    private readonly Mock<IRepository<InventoryTransaction>> _transactionRepository = new();
    private readonly Mock<IRepository<InventoryAdjustment>> _adjustmentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Product>()).Returns(_productRepository.Object);
        _unitOfWork.Setup(u => u.Repository<InventoryTransaction>()).Returns(_transactionRepository.Object);
        _unitOfWork.Setup(u => u.Repository<InventoryAdjustment>()).Returns(_adjustmentRepository.Object);

        _adjustmentRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryAdjustment>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryAdjustment, CancellationToken>((a, _) => a.Id = 50)
            .Returns(Task.CompletedTask);

        _productRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Product { Id = 1, Name = "Rice 5kg", Code = "P001", CurrentStock = 10 }]);

        _sut = new InventoryService(_unitOfWork.Object);
    }

    [Fact]
    public async Task RecordTransactionAsync_throws_NotFoundException_when_the_product_does_not_exist()
    {
        _productRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.RecordTransactionAsync(99, InventoryTransactionType.Purchase, 5, "Purchase", 1));
    }

    [Fact]
    public async Task RecordTransactionAsync_increases_stock_and_writes_a_matching_transaction()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        InventoryTransaction? captured = null;
        _transactionRepository
            .Setup(r => r.AddAsync(It.IsAny<InventoryTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryTransaction, CancellationToken>((t, _) => captured = t)
            .Returns(Task.CompletedTask);

        await _sut.RecordTransactionAsync(1, InventoryTransactionType.Purchase, 5, "Purchase", 100, "note");

        Assert.Equal(15, product.CurrentStock);
        _productRepository.Verify(r => r.Update(product), Times.Once);

        Assert.NotNull(captured);
        Assert.Equal(1, captured!.ProductId);
        Assert.Equal(InventoryTransactionType.Purchase, captured.TransactionType);
        Assert.Equal(5, captured.QuantityChange);
        Assert.Equal(15, captured.BalanceAfter);
        Assert.Equal("Purchase", captured.ReferenceType);
        Assert.Equal(100, captured.ReferenceId);
        Assert.Equal("note", captured.Notes);
    }

    [Fact]
    public async Task RecordTransactionAsync_decreases_stock_for_a_negative_quantity_change()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.RecordTransactionAsync(1, InventoryTransactionType.PurchaseReturn, -4, "PurchaseReturn", 1);

        Assert.Equal(6, product.CurrentStock);
    }

    [Fact]
    public async Task RecordTransactionAsync_throws_BusinessRuleException_when_stock_would_go_negative()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 3 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.RecordTransactionAsync(1, InventoryTransactionType.Adjustment, -4, "Adjustment", 1));

        Assert.Equal(3, product.CurrentStock);
        _productRepository.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<InventoryTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordTransactionAsync_never_calls_SaveChangesAsync()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await _sut.RecordTransactionAsync(1, InventoryTransactionType.Purchase, 5, "Purchase", 1);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAdjustmentAsync_computes_the_delta_from_the_counted_quantity()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _sut.RecordAdjustmentAsync(
            new StockAdjustmentRequestDto { ProductId = 1, NewStockCount = 7, Reason = "Recount" });

        Assert.Equal(7, product.CurrentStock);
        Assert.Equal(-3, result.QuantityChange);
        Assert.Equal("Adjustment", result.AdjustmentType);
        Assert.Equal("Rice 5kg", result.ProductName);
    }

    [Fact]
    public async Task RecordAdjustmentAsync_throws_NotFoundException_when_the_product_does_not_exist()
    {
        _productRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.RecordAdjustmentAsync(new StockAdjustmentRequestDto { ProductId = 99, NewStockCount = 5, Reason = "Recount" }));
    }

    [Fact]
    public async Task RecordDamagedAsync_applies_a_negative_quantity_change()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 10 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        var result = await _sut.RecordDamagedAsync(
            new DamagedStockRequestDto { ProductId = 1, Quantity = 3, Reason = "Broken in transit" });

        Assert.Equal(7, product.CurrentStock);
        Assert.Equal(-3, result.QuantityChange);
        Assert.Equal("Damaged", result.AdjustmentType);
    }

    [Fact]
    public async Task RecordDamagedAsync_throws_BusinessRuleException_and_saves_nothing_when_quantity_exceeds_current_stock()
    {
        var product = new Product { Id = 1, Name = "Rice 5kg", CurrentStock = 3 };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(product);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.RecordDamagedAsync(new DamagedStockRequestDto { ProductId = 1, Quantity = 10, Reason = "Flood" }));

        // Regression guard: the InventoryAdjustment row must never be persisted when the paired stock
        // change is invalid - otherwise it would be an orphaned document with no matching transaction.
        _adjustmentRepository.Verify(r => r.AddAsync(It.IsAny<InventoryAdjustment>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(3, product.CurrentStock);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_defaults_to_TransactionDate_descending_when_no_sort_is_requested()
    {
        PagedRequest? capturedRequest = null;
        _transactionRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<InventoryTransaction, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<InventoryTransaction, bool>>?, CancellationToken>((r, _, _) => capturedRequest = r)
            .ReturnsAsync(new PagedResult<InventoryTransaction> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetTransactionHistoryAsync(new PagedRequest());

        Assert.NotNull(capturedRequest);
        Assert.Equal(nameof(InventoryTransaction.TransactionDate), capturedRequest!.SortBy);
        Assert.True(capturedRequest.SortDescending);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_maps_product_names_via_lookups()
    {
        _transactionRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<InventoryTransaction, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<InventoryTransaction>
            {
                Items = [new InventoryTransaction { Id = 1, ProductId = 1, TransactionType = InventoryTransactionType.Purchase, QuantityChange = 5, BalanceAfter = 15 }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetTransactionHistoryAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Rice 5kg", dto.ProductName);
        Assert.Equal("P001", dto.ProductCode);
    }
}
