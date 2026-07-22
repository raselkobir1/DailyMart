using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Inventory;
using DailyMart.Application.Suppliers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Suppliers;

namespace DailyMart.Application.Purchases;

public class PurchaseReturnService : IPurchaseReturnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly ISupplierService _supplierService;

    public PurchaseReturnService(IUnitOfWork unitOfWork, IInventoryService inventoryService, ISupplierService supplierService)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _supplierService = supplierService;
    }

    public async Task<PagedResult<PurchaseReturnDto>> GetPagedAsync(
        long purchaseId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Repository<Purchase>().ExistsAsync(p => p.Id == purchaseId, cancellationToken))
        {
            throw new NotFoundException(nameof(Purchase), purchaseId);
        }

        var result = await _unitOfWork.Repository<PurchaseReturn>()
            .GetPagedAsync(request, r => r.PurchaseId == purchaseId, cancellationToken);

        var returnIds = result.Items.Select(r => r.Id).ToList();
        var items = await _unitOfWork.Repository<PurchaseReturnItem>()
            .FindAsync(i => returnIds.Contains(i.PurchaseReturnId), cancellationToken);
        var itemsByReturn = items.GroupBy(i => i.PurchaseReturnId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PurchaseReturnItem>)g.ToList());

        var lookups = await BuildLookupsAsync(items, cancellationToken);

        return new PagedResult<PurchaseReturnDto>
        {
            Items = result.Items
                .Select(r => r.ToDto(itemsByReturn.GetValueOrDefault(r.Id, Array.Empty<PurchaseReturnItem>()), lookups))
                .ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<PurchaseReturnDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var purchaseReturn = await GetEntityAsync(id, cancellationToken);
        var items = await _unitOfWork.Repository<PurchaseReturnItem>()
            .FindAsync(i => i.PurchaseReturnId == id, cancellationToken);
        var lookups = await BuildLookupsAsync(items, cancellationToken);

        return purchaseReturn.ToDto(items, lookups);
    }

    public async Task<PurchaseReturnDto> CreateAsync(
        long purchaseId, PurchaseReturnRequestDto request, CancellationToken cancellationToken = default)
    {
        var items = request.Items.ToEntities();

        var purchase = await _unitOfWork.Repository<Purchase>().GetByIdAsync(purchaseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Purchase), purchaseId);

        var purchaseItemRepository = _unitOfWork.Repository<PurchaseItem>();
        var returnItemRepository = _unitOfWork.Repository<PurchaseReturnItem>();

        // Keyed by PurchaseItemId so the second loop (posting inventory transactions) can recover each
        // item's ProductId - PurchaseReturnItem itself has no ProductId column, only PurchaseItemId.
        var originalItems = new Dictionary<long, PurchaseItem>();

        foreach (var item in items)
        {
            var originalItem = await purchaseItemRepository.GetByIdAsync(item.PurchaseItemId, cancellationToken);
            if (originalItem is null || originalItem.PurchaseId != purchase.Id)
            {
                throw new BusinessRuleException(
                    $"Purchase item '{item.PurchaseItemId}' does not belong to purchase '{purchase.Id}'.");
            }

            var alreadyReturned = (await returnItemRepository
                .FindAsync(r => r.PurchaseItemId == item.PurchaseItemId, cancellationToken))
                .Sum(r => r.Quantity);
            var remaining = originalItem.Quantity - alreadyReturned;

            if (item.Quantity <= 0 || item.Quantity > remaining)
            {
                throw new BusinessRuleException(
                    $"Cannot return {item.Quantity} of purchase item '{item.PurchaseItemId}' - " +
                    $"only {remaining} remains returnable.");
            }

            item.UnitPrice = originalItem.UnitPrice;
            item.LineTotal = item.Quantity * item.UnitPrice;
            originalItems[item.PurchaseItemId] = originalItem;
        }

        var purchaseReturn = request.ToEntity(purchaseId);
        purchaseReturn.TotalAmount = items.Sum(i => i.LineTotal);

        var returnRepository = _unitOfWork.Repository<PurchaseReturn>();
        await returnRepository.AddAsync(purchaseReturn, cancellationToken);
        // Saved now so purchaseReturn.Id is populated before its items reference it - same two-phase
        // reasoning as Purchase/Supplier creation.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var item in items)
        {
            item.PurchaseReturnId = purchaseReturn.Id;
            await returnItemRepository.AddAsync(item, cancellationToken);

            await _inventoryService.RecordTransactionAsync(
                originalItems[item.PurchaseItemId].ProductId,
                InventoryTransactionType.PurchaseReturn,
                -item.Quantity,
                nameof(PurchaseReturn),
                purchaseReturn.Id,
                notes: null,
                cancellationToken);
        }

        if (purchaseReturn.TotalAmount != 0)
        {
            await _supplierService.AdjustDueAsync(
                purchase.SupplierId,
                -purchaseReturn.TotalAmount,
                SupplierLedgerEntryType.PurchaseReturn,
                $"Purchase return #{PurchaseNumberFormatter.FormatReturn(purchaseReturn.Id)} " +
                $"against purchase #{PurchaseNumberFormatter.FormatPurchase(purchase.Id)}",
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lookups = await BuildLookupsAsync(items, cancellationToken);
        return purchaseReturn.ToDto(items, lookups);
    }

    private async Task<PurchaseReturn> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<PurchaseReturn>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseReturn), id);

    private async Task<PurchaseReturnLookups> BuildLookupsAsync(
        IReadOnlyCollection<PurchaseReturnItem> items, CancellationToken cancellationToken)
    {
        var purchaseItemIds = items.Select(i => i.PurchaseItemId).Distinct().ToList();
        var purchaseItems = await _unitOfWork.Repository<PurchaseItem>()
            .FindAsync(i => purchaseItemIds.Contains(i.Id), cancellationToken);

        var productIds = purchaseItems.Select(i => i.ProductId).Distinct().ToList();
        var productNames = (await _unitOfWork.Repository<Product>()
            .FindAsync(p => productIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id, p => p.Name);

        var map = purchaseItems.ToDictionary(
            pi => pi.Id,
            pi => (pi.ProductId, productNames.GetValueOrDefault(pi.ProductId, string.Empty)));

        return new PurchaseReturnLookups(map);
    }
}
