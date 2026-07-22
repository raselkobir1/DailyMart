using DailyMart.Domain.Common;

namespace DailyMart.Domain.Inventory;

/// <summary>
/// The backing document for a manual stock movement (one not caused by a Purchase/Sale) - gives
/// "Adjustment"/"Damaged" InventoryTransaction rows a real ReferenceType/ReferenceId to point to, same
/// reasoning as Purchase/PurchaseReturn existing alongside the shared InventoryTransaction log.
/// Create + read only - never edited after creation, same principle as every other posted stock/financial
/// event in this project.
/// </summary>
public class InventoryAdjustment : AuditableEntity
{
    public long ProductId { get; set; }

    /// <summary>Only Adjustment or Damaged are valid here - reuses InventoryTransactionType's vocabulary
    /// rather than a second, narrower enum; InventoryService rejects any other value.</summary>
    public InventoryTransactionType AdjustmentType { get; set; }

    /// <summary>Signed - the final computed delta. Either sign for Adjustment (a recount can go either
    /// way); always negative for Damaged.</summary>
    public decimal QuantityChange { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset AdjustmentDate { get; set; }
}
