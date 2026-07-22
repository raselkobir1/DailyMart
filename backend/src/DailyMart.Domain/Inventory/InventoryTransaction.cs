using DailyMart.Domain.Common;

namespace DailyMart.Domain.Inventory;

/// <summary>
/// One row per stock-affecting event, for any product, from any module - the general mechanism
/// CLAUDE.md §5 requires ("every stock-affecting table change also writes an InventoryTransaction row").
/// Never edited after creation - a correction is always another row, same principle as the supplier ledger.
/// </summary>
public class InventoryTransaction : AuditableEntity
{
    public long ProductId { get; set; }

    public InventoryTransactionType TransactionType { get; set; }

    /// <summary>Signed: positive is stock in, negative is stock out.</summary>
    public decimal QuantityChange { get; set; }

    /// <summary>Running stock snapshot immediately after this transaction - same pattern as the
    /// supplier ledger's BalanceAfter.</summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>E.g. "Purchase", "PurchaseReturn" - what business document caused this movement.</summary>
    public string ReferenceType { get; set; } = string.Empty;

    public long ReferenceId { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset TransactionDate { get; set; }
}
