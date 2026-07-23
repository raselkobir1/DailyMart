using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Customers;
using DailyMart.Application.Inventory;
using DailyMart.Domain.Common;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;
using DailyMart.Domain.Sales;

namespace DailyMart.Application.Sales;

public class SaleService : ISaleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;
    private readonly ICustomerService _customerService;

    public SaleService(IUnitOfWork unitOfWork, IInventoryService inventoryService, ICustomerService customerService)
    {
        _unitOfWork = unitOfWork;
        _inventoryService = inventoryService;
        _customerService = customerService;
    }

    public async Task<PagedResult<SaleDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Sale, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : sale => sale.Notes != null && sale.Notes.Contains(request.SearchTerm);

        var result = await _unitOfWork.Repository<Sale>().GetPagedAsync(request, predicate, cancellationToken);

        var saleIds = result.Items.Select(s => s.Id).ToList();
        var items = await _unitOfWork.Repository<SaleItem>()
            .FindAsync(i => saleIds.Contains(i.SaleId), cancellationToken);
        var itemsBySale = items.GroupBy(i => i.SaleId).ToDictionary(g => g.Key, g => (IReadOnlyList<SaleItem>)g.ToList());

        var lookups = await BuildLookupsAsync(result.Items, items, cancellationToken);

        return new PagedResult<SaleDto>
        {
            Items = result.Items
                .Select(s => s.ToDto(itemsBySale.GetValueOrDefault(s.Id, Array.Empty<SaleItem>()), lookups))
                .ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<SaleDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var sale = await GetEntityAsync(id, cancellationToken);
        var items = await GetItemsInternalAsync(id, cancellationToken);
        var lookups = await BuildLookupsAsync([sale], items, cancellationToken);

        return sale.ToDto(items, lookups);
    }

    public async Task<SaleDto> CreateAsync(SaleRequestDto request, CancellationToken cancellationToken = default)
    {
        var sale = request.ToEntity();
        var items = request.Items.ToEntities();

        EnsureCustomerRequiredForCreditOrPartial(sale);

        if (sale.CustomerId is not null)
        {
            await EnsureCustomerExistsAsync(sale.CustomerId.Value, cancellationToken);
        }

        var products = await GetProductsAsync(items.Select(i => i.ProductId), cancellationToken);
        EnsureProductsExist(items.Select(i => i.ProductId), products);

        ComputeAmounts(sale, items, products);

        var saleRepository = _unitOfWork.Repository<Sale>();
        await saleRepository.AddAsync(sale, cancellationToken);
        // Saved now so sale.Id is populated before its items/inventory transactions/ledger entry reference
        // it - same two-phase reasoning as Purchase (Module 7).
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await ApplyItemsAndSideEffectsAsync(sale, items, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lookups = await BuildLookupsAsync([sale], items, cancellationToken);
        return sale.ToDto(items, lookups);
    }

    /// <summary>Adds each item (setting its SaleId) plus its matching negative InventoryTransaction, then
    /// one AdjustDueAsync call if a customer is attached and anything is owed.</summary>
    private async Task ApplyItemsAndSideEffectsAsync(
        Sale sale, IReadOnlyList<SaleItem> items, CancellationToken cancellationToken)
    {
        var itemRepository = _unitOfWork.Repository<SaleItem>();

        foreach (var item in items)
        {
            item.SaleId = sale.Id;
            await itemRepository.AddAsync(item, cancellationToken);

            await _inventoryService.RecordTransactionAsync(
                item.ProductId,
                InventoryTransactionType.Sale,
                -item.Quantity,
                nameof(Sale),
                sale.Id,
                notes: null,
                cancellationToken);
        }

        if (sale.CustomerId is not null && sale.DueAmount != 0)
        {
            await _customerService.AdjustDueAsync(
                sale.CustomerId.Value,
                sale.DueAmount,
                CustomerLedgerEntryType.Sale,
                $"Sale #{SaleNumberFormatter.FormatSale(sale.Id)}",
                cancellationToken);
        }
    }

    private static void EnsureCustomerRequiredForCreditOrPartial(Sale sale)
    {
        if (sale.PaymentType != PaymentType.Cash && sale.CustomerId is null)
        {
            throw new BusinessRuleException("A customer is required for Credit or Partial sales.");
        }
    }

    /// <summary>Sets each item's UnitCost (snapshotted from Product.PurchasePrice) and LineTotal, then the
    /// header's Subtotal/Total/TotalCost/ProfitAmount/Paid/Due amounts. PaidAmount is derived from
    /// PaymentType rather than trusted verbatim from the caller - identical reasoning to
    /// PurchaseService.ComputeAmounts.</summary>
    private static void ComputeAmounts(Sale sale, IReadOnlyList<SaleItem> items, IReadOnlyDictionary<long, Product> products)
    {
        foreach (var item in items)
        {
            item.UnitCost = products[item.ProductId].PurchasePrice;
            item.LineTotal = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
        }

        sale.SubtotalAmount = items.Sum(i => i.LineTotal);
        sale.TotalAmount = sale.SubtotalAmount - sale.DiscountAmount + sale.VatAmount;
        sale.TotalCost = items.Sum(i => i.Quantity * i.UnitCost);
        sale.ProfitAmount = sale.TotalAmount - sale.TotalCost;

        sale.PaidAmount = sale.PaymentType switch
        {
            PaymentType.Cash => sale.TotalAmount,
            PaymentType.Credit => 0m,
            PaymentType.Partial => ValidatePartialPaidAmount(sale.PaidAmount, sale.TotalAmount),
            _ => throw new BusinessRuleException($"Unknown payment type '{sale.PaymentType}'.")
        };

        sale.DueAmount = sale.TotalAmount - sale.PaidAmount;
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

    private async Task<Sale> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Sale>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Sale), id);

    private async Task<List<SaleItem>> GetItemsInternalAsync(long saleId, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<SaleItem>().FindAsync(i => i.SaleId == saleId, cancellationToken);

    private async Task EnsureCustomerExistsAsync(long customerId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Customer>().ExistsAsync(c => c.Id == customerId, cancellationToken))
        {
            throw new BusinessRuleException($"Customer with id '{customerId}' does not exist.");
        }
    }

    private async Task<Dictionary<long, Product>> GetProductsAsync(
        IEnumerable<long> productIds, CancellationToken cancellationToken)
    {
        var distinctIds = productIds.Distinct().ToList();

        var products = await _unitOfWork.Repository<Product>()
            .FindAsync(p => distinctIds.Contains(p.Id), cancellationToken);

        return products.ToDictionary(p => p.Id);
    }

    private static void EnsureProductsExist(IEnumerable<long> productIds, IReadOnlyDictionary<long, Product> products)
    {
        var distinctCount = productIds.Distinct().Count();

        if (products.Count != distinctCount)
        {
            throw new BusinessRuleException("One or more products in the sale do not exist.");
        }
    }

    private async Task<SaleLookups> BuildLookupsAsync(
        IReadOnlyCollection<Sale> sales, IReadOnlyCollection<SaleItem> items, CancellationToken cancellationToken)
    {
        var customerIds = sales.Where(s => s.CustomerId is not null).Select(s => s.CustomerId!.Value).Distinct().ToList();
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();

        var customers = await _unitOfWork.Repository<Customer>()
            .FindAsync(c => customerIds.Contains(c.Id), cancellationToken);
        var products = await _unitOfWork.Repository<Product>()
            .FindAsync(p => productIds.Contains(p.Id), cancellationToken);

        return new SaleLookups(
            customers.ToDictionary(c => c.Id, c => c.Name),
            products.ToDictionary(p => p.Id, p => (p.Name, p.Code)));
    }
}
