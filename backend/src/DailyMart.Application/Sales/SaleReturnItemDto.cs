namespace DailyMart.Application.Sales;

public class SaleReturnItemDto
{
    public long Id { get; init; }

    public long SaleItemId { get; init; }

    /// <summary>Denormalized from the original SaleItem - SaleReturnItem itself has no ProductId column.</summary>
    public long ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}
