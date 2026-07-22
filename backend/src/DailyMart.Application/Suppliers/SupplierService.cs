using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Suppliers;

namespace DailyMart.Application.Suppliers;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Supplier> Repository => _unitOfWork.Repository<Supplier>();

    private IRepository<SupplierLedgerEntry> LedgerRepository => _unitOfWork.Repository<SupplierLedgerEntry>();

    public async Task<PagedResult<SupplierDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Supplier, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : supplier => supplier.Name.Contains(request.SearchTerm);

        var result = await Repository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<SupplierDto>
        {
            Items = result.Items.Select(s => s.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<SupplierDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<SupplierDto> CreateAsync(
        CreateSupplierRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var supplier = request.ToEntity();
        supplier.CurrentDue = supplier.OpeningBalance;

        await Repository.AddAsync(supplier, cancellationToken);
        // Saved now (rather than in one call with the ledger entry below) so supplier.Id is populated by
        // the database before it's used as the ledger entry's foreign key - there's no navigation
        // property EF could use to fix that up automatically within a single SaveChanges call.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (supplier.OpeningBalance != 0)
        {
            var openingEntry = new SupplierLedgerEntry
            {
                SupplierId = supplier.Id,
                EntryType = SupplierLedgerEntryType.OpeningBalance,
                Description = "Opening balance",
                Amount = supplier.OpeningBalance,
                BalanceAfter = supplier.OpeningBalance,
                TransactionDate = DateTimeOffset.UtcNow
            };

            await LedgerRepository.AddAsync(openingEntry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return supplier.ToDto();
    }

    public async Task<SupplierDto> UpdateAsync(
        long id, SupplierRequestDto request, CancellationToken cancellationToken = default)
    {
        var supplier = await GetEntityAsync(id, cancellationToken);

        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        request.ApplyTo(supplier);

        Repository.Update(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return supplier.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var supplier = await GetEntityAsync(id, cancellationToken);

        Repository.Remove(supplier);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<SupplierLedgerEntryDto>> GetLedgerAsync(
        long supplierId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        if (!await Repository.ExistsAsync(s => s.Id == supplierId, cancellationToken))
        {
            throw new NotFoundException(nameof(Supplier), supplierId);
        }

        var effectiveRequest = string.IsNullOrWhiteSpace(request.SortBy)
            ? new PagedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortBy = nameof(SupplierLedgerEntry.TransactionDate),
                SortDescending = false
            }
            : request;

        var result = await LedgerRepository.GetPagedAsync(
            effectiveRequest, entry => entry.SupplierId == supplierId, cancellationToken);

        return new PagedResult<SupplierLedgerEntryDto>
        {
            Items = result.Items.Select(e => e.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task AdjustDueAsync(
        long supplierId,
        decimal amount,
        SupplierLedgerEntryType entryType,
        string description,
        CancellationToken cancellationToken = default)
    {
        var supplier = await GetEntityAsync(supplierId, cancellationToken);

        supplier.CurrentDue += amount;
        Repository.Update(supplier);

        var entry = new SupplierLedgerEntry
        {
            SupplierId = supplierId,
            EntryType = entryType,
            Description = description,
            Amount = amount,
            BalanceAfter = supplier.CurrentDue,
            TransactionDate = DateTimeOffset.UtcNow
        };

        await LedgerRepository.AddAsync(entry, cancellationToken);
    }

    private async Task<Supplier> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Supplier), id);

    private async Task EnsureNameIsUniqueAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var duplicateExists = await Repository.ExistsAsync(
            supplier => supplier.Name.ToLower() == normalizedName && (excludeId == null || supplier.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A supplier named '{name}' already exists.");
        }
    }
}
