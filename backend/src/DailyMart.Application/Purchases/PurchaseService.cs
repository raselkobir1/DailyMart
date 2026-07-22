using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Inventory;
using DailyMart.Application.Suppliers;
using DailyMart.Domain.Common;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Suppliers;

namespace DailyMart.Application.Purchases;

public class PurchaseService : IPurchaseService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly ISupplierService _supplierService;

    public PurchaseService(IUnitOfWork unitOfWork, IInventoryService inventoryService, ISupplierService supplierService)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _supplierService = supplierService;
    }

    public async Task<PagedResult<PurchaseDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Purchase, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : purchase => purchase.Notes != null && purchase.Notes.Contains(request.SearchTerm);

        var result = await _unitOfWork.Repository<Purchase>().GetPagedAsync(request, predicate, cancellationToken);

        var purchaseIds = result.Items.Select(p => p.Id).ToList();
        var items = await _unitOfWork.Repository<PurchaseItem>()
            .FindAsync(i => purchaseIds.Contains(i.PurchaseId), cancellationToken);
        var itemsByPurchase = items.GroupBy(i => i.PurchaseId).ToDictionary(g => g.Key, g => (IReadOnlyList<PurchaseItem>)g.ToList());

        var lookups = await BuildLookupsAsync(result.Items, items, cancellationToken);

        return new PagedResult<PurchaseDto>
        {
            Items = result.Items
                .Select(p => p.ToDto(itemsByPurchase.GetValueOrDefault(p.Id, Array.Empty<PurchaseItem>()), lookups))
                .ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<PurchaseDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var purchase = await GetEntityAsync(id, cancellationToken);
        var items = await GetItemsInternalAsync(id, cancellationToken);
        var lookups = await BuildLookupsAsync([purchase], items, cancellationToken);

        return purchase.ToDto(items, lookups);
    }

    public async Task<PurchaseDto> CreateAsync(
        PurchaseRequestDto request, CancellationToken cancellationToken = default)
    {
        var purchase = request.ToEntity();
        var items = request.Items.ToEntities();

        await EnsureSupplierExistsAsync(purchase.SupplierId, cancellationToken);
        await EnsureProductsExistAsync(items.Select(i => i.ProductId), cancellationToken);

        ComputeAmounts(purchase, items);

        var purchaseRepository = _unitOfWork.Repository<Purchase>();
        await purchaseRepository.AddAsync(purchase, cancellationToken);
        // Saved now so purchase.Id is populated before its items/inventory transactions/ledger entry
        // reference it - same two-phase reasoning as Supplier's opening balance (Module 5).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await ApplyItemsAndSideEffectsAsync(purchase, items, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lookups = await BuildLookupsAsync([purchase], items, cancellationToken);
        return purchase.ToDto(items, lookups);
    }

    public async Task<PurchaseDto> UpdateAsync(
        long id, PurchaseRequestDto request, CancellationToken cancellationToken = default)
    {
        var items = request.Items.ToEntities();

        var existing = await GetEntityAsync(id, cancellationToken);
        var oldItems = await GetItemsInternalAsync(id, cancellationToken);

        await ReverseEffectsAsync(existing, oldItems, cancellationToken);

        await EnsureSupplierExistsAsync(request.SupplierId, cancellationToken);
        await EnsureProductsExistAsync(items.Select(i => i.ProductId), cancellationToken);

        var itemRepository = _unitOfWork.Repository<PurchaseItem>();
        foreach (var oldItem in oldItems)
        {
            itemRepository.Remove(oldItem);
        }

        existing.SupplierId = request.SupplierId;
        existing.PurchaseDate = request.PurchaseDate;
        existing.PaymentType = request.PaymentType;
        existing.DiscountAmount = request.DiscountAmount;
        existing.VatAmount = request.VatAmount;
        existing.PaidAmount = request.PaidAmount;
        existing.Notes = request.Notes;

        ComputeAmounts(existing, items);

        _unitOfWork.Repository<Purchase>().Update(existing);

        await ApplyItemsAndSideEffectsAsync(existing, items, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lookups = await BuildLookupsAsync([existing], items, cancellationToken);
        return existing.ToDto(items, lookups);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var purchase = await GetEntityAsync(id, cancellationToken);
        var items = await GetItemsInternalAsync(id, cancellationToken);

        await ReverseEffectsAsync(purchase, items, cancellationToken);

        var itemRepository = _unitOfWork.Repository<PurchaseItem>();
        foreach (var item in items)
        {
            itemRepository.Remove(item);
        }

        _unitOfWork.Repository<Purchase>().Remove(purchase);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Adds each item (setting its PurchaseId) plus its matching InventoryTransaction, then one
    /// AdjustDueAsync call if anything is owed - shared by CreateAsync and UpdateAsync's reapply phase.</summary>
    private async Task ApplyItemsAndSideEffectsAsync(
        Purchase purchase, IReadOnlyList<PurchaseItem> items, CancellationToken cancellationToken)
    {
        var itemRepository = _unitOfWork.Repository<PurchaseItem>();

        foreach (var item in items)
        {
            item.PurchaseId = purchase.Id;
            await itemRepository.AddAsync(item, cancellationToken);

            await _inventoryService.RecordTransactionAsync(
                item.ProductId,
                InventoryTransactionType.Purchase,
                item.Quantity,
                nameof(Purchase),
                purchase.Id,
                notes: null,
                cancellationToken);
        }

        if (purchase.DueAmount != 0)
        {
            await _supplierService.AdjustDueAsync(
                purchase.SupplierId,
                purchase.DueAmount,
                SupplierLedgerEntryType.Purchase,
                $"Purchase #{PurchaseNumberFormatter.FormatPurchase(purchase.Id)}",
                cancellationToken);
        }
    }

    /// <summary>Undoes a purchase's original stock/due effects with new, visible correction rows - shared
    /// by UpdateAsync (before reapplying) and DeleteAsync.</summary>
    private async Task ReverseEffectsAsync(
        Purchase purchase, IReadOnlyList<PurchaseItem> items, CancellationToken cancellationToken)
    {
        var purchaseNumber = PurchaseNumberFormatter.FormatPurchase(purchase.Id);

        foreach (var item in items)
        {
            await _inventoryService.RecordTransactionAsync(
                item.ProductId,
                InventoryTransactionType.Adjustment,
                -item.Quantity,
                nameof(Purchase),
                purchase.Id,
                $"Reversal: Purchase #{purchaseNumber} updated",
                cancellationToken);
        }

        if (purchase.DueAmount != 0)
        {
            await _supplierService.AdjustDueAsync(
                purchase.SupplierId,
                -purchase.DueAmount,
                SupplierLedgerEntryType.Adjustment,
                $"Reversal: Purchase #{purchaseNumber} updated",
                cancellationToken);
        }
    }

    /// <summary>Sets each item's LineTotal and the header's Subtotal/Total/Paid/Due amounts. PaidAmount is
    /// derived from PaymentType rather than trusted verbatim from the caller: Cash always pays the full
    /// total, Credit always pays nothing, and only Partial takes the caller's PaidAmount (validated to be
    /// strictly between 0 and the total).</summary>
    private static void ComputeAmounts(Purchase purchase, IReadOnlyList<PurchaseItem> items)
    {
        foreach (var item in items)
        {
            item.LineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
        }

        purchase.SubtotalAmount = items.Sum(i => i.LineTotal);
        purchase.TotalAmount = purchase.SubtotalAmount - purchase.DiscountAmount + purchase.VatAmount;

        purchase.PaidAmount = purchase.PaymentType switch
        {
            PaymentType.Cash => purchase.TotalAmount,
            PaymentType.Credit => 0m,
            PaymentType.Partial => ValidatePartialPaidAmount(purchase.PaidAmount, purchase.TotalAmount),
            _ => throw new BusinessRuleException($"Unknown payment type '{purchase.PaymentType}'.")
        };

        purchase.DueAmount = purchase.TotalAmount - purchase.PaidAmount;
    }

    private static decimal ValidatePartialPaidAmount(decimal paidAmount, decimal totalAmount)
    {
        if (paidAmount <= 0 || paidAmount >= totalAmount)
        {
            throw new BusinessRuleException(
                $"A partial payment must be greater than 0 and less than the total amount ({totalAmount}).");
        }

        return paidAmount;
    }

    private async Task<Purchase> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Purchase>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Purchase), id);

    private async Task<List<PurchaseItem>> GetItemsInternalAsync(long purchaseId, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<PurchaseItem>().FindAsync(i => i.PurchaseId == purchaseId, cancellationToken);

    private async Task EnsureSupplierExistsAsync(long supplierId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Supplier>().ExistsAsync(s => s.Id == supplierId, cancellationToken))
        {
            throw new BusinessRuleException($"Supplier with id '{supplierId}' does not exist.");
        }
    }

    private async Task EnsureProductsExistAsync(IEnumerable<long> productIds, CancellationToken cancellationToken)
    {
        var distinctIds = productIds.Distinct().ToList();

        var existing = await _unitOfWork.Repository<Product>()
            .FindAsync(p => distinctIds.Contains(p.Id), cancellationToken);

        if (existing.Count != distinctIds.Count)
        {
            throw new BusinessRuleException("One or more products in the purchase do not exist.");
        }
    }

    private async Task<PurchaseLookups> BuildLookupsAsync(
        IReadOnlyCollection<Purchase> purchases, IReadOnlyCollection<PurchaseItem> items, CancellationToken cancellationToken)
    {
        var supplierIds = purchases.Select(p => p.SupplierId).Distinct().ToList();
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var suppliers = await _unitOfWork.Repository<Supplier>()
            .FindAsync(s => supplierIds.Contains(s.Id), cancellationToken);
        var products = await _unitOfWork.Repository<Product>()
            .FindAsync(p => productIds.Contains(p.Id), cancellationToken);

        return new PurchaseLookups(
            suppliers.ToDictionary(s => s.Id, s => s.Name),
            products.ToDictionary(p => p.Id, p => (p.Name, p.Code)));
    }
}
