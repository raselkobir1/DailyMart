using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.Products;

namespace DailyMart.Application.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task RecordTransactionAsync(
        long productId,
        InventoryTransactionType transactionType,
        decimal quantityChange,
        string referenceType,
        long referenceId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var productRepository = _unitOfWork.Repository<Product>();

        var product = await productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        var newStock = product.CurrentStock + quantityChange;
        if (newStock < 0)
        {
            throw new BusinessRuleException(
                $"This would reduce '{product.Name}' stock below zero " +
                $"(current: {product.CurrentStock}, change: {quantityChange}).");
        }

        product.CurrentStock = newStock;
        productRepository.Update(product);

        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            TransactionType = transactionType,
            QuantityChange = quantityChange,
            BalanceAfter = newStock,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            TransactionDate = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Repository<InventoryTransaction>().AddAsync(transaction, cancellationToken);
    }

    public async Task<InventoryAdjustmentDto> RecordAdjustmentAsync(
        StockAdjustmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var quantityChange = request.NewStockCount - product.CurrentStock;

        var adjustment = await CreateAdjustmentAsync(
            request.ProductId, InventoryTransactionType.Adjustment, quantityChange, request.Reason, cancellationToken);

        var lookups = await BuildLookupsAsync([adjustment.ProductId], cancellationToken);
        return adjustment.ToDto(lookups);
    }

    public async Task<InventoryAdjustmentDto> RecordDamagedAsync(
        DamagedStockRequestDto request, CancellationToken cancellationToken = default)
    {
        var adjustment = await CreateAdjustmentAsync(
            request.ProductId, InventoryTransactionType.Damaged, -request.Quantity, request.Reason, cancellationToken);

        var lookups = await BuildLookupsAsync([adjustment.ProductId], cancellationToken);
        return adjustment.ToDto(lookups);
    }

    public async Task<PagedResult<InventoryTransactionDto>> GetTransactionHistoryAsync(
        PagedRequest request, long? productId = null, CancellationToken cancellationToken = default)
    {
        Expression<Func<InventoryTransaction, bool>>? predicate = productId is null
            ? null
            : t => t.ProductId == productId;

        var effectiveRequest = string.IsNullOrWhiteSpace(request.SortBy)
            ? new PagedRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortBy = nameof(InventoryTransaction.TransactionDate),
                SortDescending = true
            }
            : request;

        var result = await _unitOfWork.Repository<InventoryTransaction>().GetPagedAsync(effectiveRequest, predicate, cancellationToken);
        var lookups = await BuildLookupsAsync(result.Items.Select(t => t.ProductId), cancellationToken);

        return new PagedResult<InventoryTransactionDto>
        {
            Items = result.Items.Select(t => t.ToDto(lookups)).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    /// <summary>Creates the InventoryAdjustment document (saved first to get its Id, same two-phase
    /// reasoning as Purchase/Supplier creation), then posts the matching InventoryTransaction and commits
    /// both - unlike RecordTransactionAsync, this is a top-level create operation, not a helper composed
    /// into a larger transaction, so it owns its own commit.</summary>
    private async Task<InventoryAdjustment> CreateAdjustmentAsync(
        long productId,
        InventoryTransactionType adjustmentType,
        decimal quantityChange,
        string reason,
        CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Repository<Product>().GetByIdAsync(productId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), productId);

        // Checked here, before the adjustment is ever saved - not a duplicate of RecordTransactionAsync's
        // own check below. Without this, a too-large Damaged quantity would have already committed the
        // InventoryAdjustment row (its Id is needed as RecordTransactionAsync's ReferenceId) by the time
        // RecordTransactionAsync throws, leaving an orphaned adjustment document with no matching
        // transaction and no stock change - a real inconsistency, not a hypothetical one.
        if (product.CurrentStock + quantityChange < 0)
        {
            throw new BusinessRuleException(
                $"This would reduce '{product.Name}' stock below zero " +
                $"(current: {product.CurrentStock}, change: {quantityChange}).");
        }

        var adjustment = new InventoryAdjustment
        {
            ProductId = productId,
            AdjustmentType = adjustmentType,
            QuantityChange = quantityChange,
            Reason = reason,
            AdjustmentDate = DateTimeOffset.UtcNow
        };

        await _unitOfWork.Repository<InventoryAdjustment>().AddAsync(adjustment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecordTransactionAsync(
            productId, adjustmentType, quantityChange, nameof(InventoryAdjustment), adjustment.Id, reason, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return adjustment;
    }

    private async Task<InventoryLookups> BuildLookupsAsync(IEnumerable<long> productIds, CancellationToken cancellationToken)
    {
        var distinctIds = productIds.Distinct().ToList();

        var products = await _unitOfWork.Repository<Product>().FindAsync(p => distinctIds.Contains(p.Id), cancellationToken);

        return new InventoryLookups(products.ToDictionary(p => p.Id, p => (p.Name, p.Code)));
    }
}
