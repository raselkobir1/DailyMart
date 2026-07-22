namespace DailyMart.Application.Inventory;

public class InventoryAdjustmentDto
{
    public long Id { get; init; }

    public long ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public string AdjustmentType { get; init; } = string.Empty;

    public decimal QuantityChange { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset AdjustmentDate { get; init; }
}
