using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Suppliers;
using DailyMart.Domain.Suppliers;
using Moq;

namespace DailyMart.UnitTests.Suppliers;

public class SupplierServiceTests
{
    private readonly Mock<IRepository<Supplier>> _supplierRepository = new();
    private readonly Mock<IRepository<SupplierLedgerEntry>> _ledgerRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SupplierService _sut;

    public SupplierServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Supplier>()).Returns(_supplierRepository.Object);
        _unitOfWork.Setup(u => u.Repository<SupplierLedgerEntry>()).Returns(_ledgerRepository.Object);

        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new SupplierService(_unitOfWork.Object);
    }

    private static CreateSupplierRequestDto ValidCreateRequest(string name = "Acme Distributors", decimal openingBalance = 0) => new()
    {
        Name = name,
        ContactPerson = "John Doe",
        Phone = "0123456789",
        OpeningBalance = openingBalance
    };

    private static SupplierRequestDto ValidUpdateRequest(string name = "Acme Distributors") => new()
    {
        Name = name,
        ContactPerson = "Jane Doe"
    };

    [Fact]
    public async Task GetPagedAsync_maps_suppliers_to_dtos()
    {
        _supplierRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Supplier, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Supplier>
            {
                Items = [new Supplier { Id = 1, Name = "Acme Distributors", CurrentDue = 500 }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Acme Distributors", dto.Name);
        Assert.Equal(500, dto.CurrentDue);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_name()
    {
        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidCreateRequest()));

        _supplierRepository.Verify(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_zero_opening_balance_creates_no_ledger_entry()
    {
        var result = await _sut.CreateAsync(ValidCreateRequest(openingBalance: 0));

        Assert.Equal(0, result.CurrentDue);
        _ledgerRepository.Verify(
            r => r.AddAsync(It.IsAny<SupplierLedgerEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_a_nonzero_opening_balance_creates_a_matching_ledger_entry_and_sets_CurrentDue()
    {
        Supplier? capturedSupplier = null;
        _supplierRepository
            .Setup(r => r.AddAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .Callback<Supplier, CancellationToken>((s, _) =>
            {
                capturedSupplier = s;
                s.Id = 42; // Simulates the database assigning the identity value on save.
            })
            .Returns(Task.CompletedTask);

        SupplierLedgerEntry? capturedEntry = null;
        _ledgerRepository
            .Setup(r => r.AddAsync(It.IsAny<SupplierLedgerEntry>(), It.IsAny<CancellationToken>()))
            .Callback<SupplierLedgerEntry, CancellationToken>((e, _) => capturedEntry = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(ValidCreateRequest(openingBalance: 1500));

        Assert.Equal(1500, result.CurrentDue);
        Assert.NotNull(capturedSupplier);
        Assert.NotNull(capturedEntry);
        Assert.Equal(42, capturedEntry!.SupplierId);
        Assert.Equal(SupplierLedgerEntryType.OpeningBalance, capturedEntry.EntryType);
        Assert.Equal(1500, capturedEntry.Amount);
        Assert.Equal(1500, capturedEntry.BalanceAfter);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_missing()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(404, ValidUpdateRequest()));
    }

    [Fact]
    public async Task UpdateAsync_never_changes_OpeningBalance_or_CurrentDue()
    {
        var existing = new Supplier { Id = 5, Name = "Acme Distributors", OpeningBalance = 1000, CurrentDue = 750 };
        _supplierRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(5, ValidUpdateRequest());

        Assert.Equal(1000, result.OpeningBalance);
        Assert.Equal(750, result.CurrentDue);
        Assert.Equal(1000, existing.OpeningBalance);
        Assert.Equal(750, existing.CurrentDue);
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_supplier_exists()
    {
        var existing = new Supplier { Id = 7, Name = "Acme Distributors" };
        _supplierRepository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _supplierRepository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_missing()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }

    [Fact]
    public async Task GetLedgerAsync_throws_NotFoundException_when_the_supplier_does_not_exist()
    {
        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetLedgerAsync(404, new PagedRequest()));
    }

    [Fact]
    public async Task GetLedgerAsync_defaults_to_TransactionDate_ascending_when_no_sort_is_requested()
    {
        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PagedRequest? capturedRequest = null;
        _ledgerRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<SupplierLedgerEntry, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<SupplierLedgerEntry, bool>>?, CancellationToken>((r, _, _) => capturedRequest = r)
            .ReturnsAsync(new PagedResult<SupplierLedgerEntry> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetLedgerAsync(1, new PagedRequest());

        Assert.NotNull(capturedRequest);
        Assert.Equal(nameof(SupplierLedgerEntry.TransactionDate), capturedRequest!.SortBy);
        Assert.False(capturedRequest.SortDescending);
    }

    [Fact]
    public async Task GetLedgerAsync_respects_an_explicitly_requested_sort()
    {
        _supplierRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Supplier, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PagedRequest? capturedRequest = null;
        _ledgerRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<SupplierLedgerEntry, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<SupplierLedgerEntry, bool>>?, CancellationToken>((r, _, _) => capturedRequest = r)
            .ReturnsAsync(new PagedResult<SupplierLedgerEntry> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetLedgerAsync(1, new PagedRequest { SortBy = "Amount", SortDescending = true });

        Assert.Equal("Amount", capturedRequest!.SortBy);
        Assert.True(capturedRequest.SortDescending);
    }

    [Fact]
    public async Task AdjustDueAsync_throws_NotFoundException_when_the_supplier_does_not_exist()
    {
        _supplierRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Supplier?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.AdjustDueAsync(404, 100, SupplierLedgerEntryType.Purchase, "Purchase #PUR-000001"));
    }

    [Fact]
    public async Task AdjustDueAsync_updates_CurrentDue_and_adds_a_matching_ledger_entry()
    {
        var supplier = new Supplier { Id = 5, Name = "Acme Distributors", CurrentDue = 200 };
        _supplierRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        SupplierLedgerEntry? captured = null;
        _ledgerRepository
            .Setup(r => r.AddAsync(It.IsAny<SupplierLedgerEntry>(), It.IsAny<CancellationToken>()))
            .Callback<SupplierLedgerEntry, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        await _sut.AdjustDueAsync(5, 300, SupplierLedgerEntryType.Purchase, "Purchase #PUR-000001");

        Assert.Equal(500, supplier.CurrentDue);
        _supplierRepository.Verify(r => r.Update(supplier), Times.Once);

        Assert.NotNull(captured);
        Assert.Equal(5, captured!.SupplierId);
        Assert.Equal(SupplierLedgerEntryType.Purchase, captured.EntryType);
        Assert.Equal("Purchase #PUR-000001", captured.Description);
        Assert.Equal(300, captured.Amount);
        Assert.Equal(500, captured.BalanceAfter);
    }

    [Fact]
    public async Task AdjustDueAsync_accepts_a_negative_amount_and_can_take_CurrentDue_below_zero()
    {
        var supplier = new Supplier { Id = 5, Name = "Acme Distributors", CurrentDue = 100 };
        _supplierRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        await _sut.AdjustDueAsync(5, -150, SupplierLedgerEntryType.PurchaseReturn, "Purchase return #PRET-000001");

        Assert.Equal(-50, supplier.CurrentDue);
    }

    [Fact]
    public async Task AdjustDueAsync_never_calls_SaveChangesAsync()
    {
        var supplier = new Supplier { Id = 5, Name = "Acme Distributors" };
        _supplierRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        await _sut.AdjustDueAsync(5, 100, SupplierLedgerEntryType.Purchase, "Purchase #PUR-000001");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
