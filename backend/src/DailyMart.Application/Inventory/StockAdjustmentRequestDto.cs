namespace DailyMart.Application.Inventory;

/// <summary>NewStockCount is the actual physical count, not a delta - InventoryService computes the
/// change from Product.CurrentStock itself (Module 8 Step 1's scope decision).</summary>
public class StockAdjustmentRequestDto
{
    public long ProductId { get; init; }

    public decimal NewStockCount { get; init; }

    public string Reason { get; init; } = string.Empty;
}
