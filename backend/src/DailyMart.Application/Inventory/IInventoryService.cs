using DailyMart.Application.Common.Models;
using DailyMart.Domain.Inventory;

namespace DailyMart.Application.Inventory;

public interface IInventoryService
{
    /// <summary>Adjusts Product.CurrentStock and writes the matching InventoryTransaction row together -
    /// the single place that keeps the two in lockstep, same reasoning as ISupplierService.AdjustDueAsync.
    /// Throws BusinessRuleException if the resulting stock would go negative. Stage-only: does not call
    /// SaveChangesAsync, so callers can compose this into a larger atomic commit.</summary>
    Task RecordTransactionAsync(
        long productId,
        InventoryTransactionType transactionType,
        decimal quantityChange,
        string referenceType,
        long referenceId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Records a physical stock count correction. The request supplies the actual counted
    /// quantity, not a delta - the service computes quantityChange = newStockCount - CurrentStock itself,
    /// so staff never has to do that subtraction by hand. Creates the InventoryAdjustment document, then
    /// posts the matching InventoryTransaction, and commits both.</summary>
    Task<InventoryAdjustmentDto> RecordAdjustmentAsync(
        StockAdjustmentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Records a write-off. Quantity is always a positive count of units damaged/lost; the
    /// service applies it as a negative stock change - Damaged can never increase stock.</summary>
    Task<InventoryAdjustmentDto> RecordDamagedAsync(
        DamagedStockRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>The full InventoryTransaction log, optionally filtered to one product. Defaults to
    /// newest-first, unlike the supplier ledger's oldest-first bank-statement style - this reads more like
    /// a recent-activity feed.</summary>
    Task<PagedResult<InventoryTransactionDto>> GetTransactionHistoryAsync(
        PagedRequest request, long? productId = null, CancellationToken cancellationToken = default);
}
