using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Customers;
using DailyMart.Application.Inventory;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Sales;

namespace DailyMart.Application.Sales;

public class SaleReturnService : ISaleReturnService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly ICustomerService _customerService;

    public SaleReturnService(IUnitOfWork unitOfWork, IInventoryService inventoryService, ICustomerService customerService)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _customerService = customerService;
    }

    public async Task<PagedResult<SaleReturnDto>> GetPagedAsync(
        long saleId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _unitOfWork.Repository<Sale>().ExistsAsync(s => s.Id == saleId, cancellationToken))
        {
            throw new NotFoundException(nameof(Sale), saleId);
        }

        var result = await _unitOfWork.Repository<SaleReturn>()
            .GetPagedAsync(request, r => r.SaleId == saleId, cancellationToken);

        var returnIds = result.Items.Select(r => r.Id).ToList();
        var items = await _unitOfWork.Repository<SaleReturnItem>()
            .FindAsync(i => returnIds.Contains(i.SaleReturnId), cancellationToken);
        var itemsByReturn = items.GroupBy(i => i.SaleReturnId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleReturnItem>)g.ToList());

        var lookups = await BuildLookupsAsync(items, cancellationToken);

        return new PagedResult<SaleReturnDto>
        {
            Items = result.Items
                .Select(r => r.ToDto(itemsByReturn.GetValueOrDefault(r.Id, Array.Empty<SaleReturnItem>()), lookups))
                .ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<SaleReturnDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var saleReturn = await GetEntityAsync(id, cancellationToken);
        var items = await _unitOfWork.Repository<SaleReturnItem>()
            .FindAsync(i => i.SaleReturnId == id, cancellationToken);
        var lookups = await BuildLookupsAsync(items, cancellationToken);

        return saleReturn.ToDto(items, lookups);
    }

    public async Task<SaleReturnDto> CreateAsync(
        long saleId, SaleReturnRequestDto request, CancellationToken cancellationToken = default)
    {
        var items = request.Items.ToEntities();

        var sale = await _unitOfWork.Repository<Sale>().GetByIdAsync(saleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), saleId);

        var saleItemRepository = _unitOfWork.Repository<SaleItem>();
        var returnItemRepository = _unitOfWork.Repository<SaleReturnItem>();

        // Keyed by SaleItemId so the second loop (posting inventory transactions) can recover each item's
        // ProductId - SaleReturnItem itself has no ProductId column, only SaleItemId.
        var originalItems = new Dictionary<long, SaleItem>();

        foreach (var item in items)
        {
            var originalItem = await saleItemRepository.GetByIdAsync(item.SaleItemId, cancellationToken);
            if (originalItem is null || originalItem.SaleId != sale.Id)
            {
                throw new BusinessRuleException(
                    $"Sale item '{item.SaleItemId}' does not belong to sale '{sale.Id}'.");
            }

            var alreadyReturned = (await returnItemRepository
                .FindAsync(r => r.SaleItemId == item.SaleItemId, cancellationToken))
                .Sum(r => r.Quantity);
            var remaining = originalItem.Quantity - alreadyReturned;

            if (item.Quantity <= 0 || item.Quantity > remaining)
            {
                throw new BusinessRuleException(
                    $"Cannot return {item.Quantity} of sale item '{item.SaleItemId}' - " +
                    $"only {remaining} remains returnable.");
            }

            item.UnitPrice = originalItem.UnitPrice;
            item.LineTotal = item.Quantity * item.UnitPrice;
            originalItems[item.SaleItemId] = originalItem;
        }

        var saleReturn = request.ToEntity(saleId);
        saleReturn.TotalAmount = items.Sum(i => i.LineTotal);

        var returnRepository = _unitOfWork.Repository<SaleReturn>();
        await returnRepository.AddAsync(saleReturn, cancellationToken);
        // Saved now so saleReturn.Id is populated before its items reference it - same two-phase reasoning
        // as Sale/PurchaseReturn creation.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var item in items)
        {
            item.SaleReturnId = saleReturn.Id;
            await returnItemRepository.AddAsync(item, cancellationToken);

            await _inventoryService.RecordTransactionAsync(
                originalItems[item.SaleItemId].ProductId,
                InventoryTransactionType.SaleReturn,
                item.Quantity,
                nameof(SaleReturn),
                saleReturn.Id,
                notes: null,
                cancellationToken);
        }

        // Only when the original sale had a customer attached - a walk-in Cash sale's return has no due to
        // reduce (see ISaleReturnService's doc comment).
        if (sale.CustomerId is not null && saleReturn.TotalAmount != 0)
        {
            await _customerService.AdjustDueAsync(
                sale.CustomerId.Value,
                -saleReturn.TotalAmount,
                CustomerLedgerEntryType.SaleReturn,
                $"Sale return #{SaleNumberFormatter.FormatReturn(saleReturn.Id)} " +
                $"against sale #{SaleNumberFormatter.FormatSale(sale.Id)}",
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lookups = await BuildLookupsAsync(items, cancellationToken);
        return saleReturn.ToDto(items, lookups);
    }

    private async Task<SaleReturn> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<SaleReturn>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SaleReturn), id);

    private async Task<SaleReturnLookups> BuildLookupsAsync(
        IReadOnlyCollection<SaleReturnItem> items, CancellationToken cancellationToken)
    {
        var saleItemIds = items.Select(i => i.SaleItemId).Distinct().ToList();
        var saleItems = await _unitOfWork.Repository<SaleItem>()
            .FindAsync(i => saleItemIds.Contains(i.Id), cancellationToken);

        var productIds = saleItems.Select(i => i.ProductId).Distinct().ToList();
        var productNames = (await _unitOfWork.Repository<Product>()
            .FindAsync(p => productIds.Contains(p.Id), cancellationToken))
            .ToDictionary(p => p.Id, p => p.Name);

        var map = saleItems.ToDictionary(
            si => si.Id,
            si => (si.ProductId, productNames.GetValueOrDefault(si.ProductId, string.Empty)));

        return new SaleReturnLookups(map);
    }
}
