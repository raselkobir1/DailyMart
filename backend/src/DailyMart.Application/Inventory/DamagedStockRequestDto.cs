namespace DailyMart.Application.Inventory;

/// <summary>Quantity is always a positive count of units damaged/lost - InventoryService applies it as a
/// negative stock change (Module 8 Step 1's scope decision).</summary>
public class DamagedStockRequestDto
{
    public long ProductId { get; init; }

    public decimal Quantity { get; init; }

    public string Reason { get; init; } = string.Empty;
}
