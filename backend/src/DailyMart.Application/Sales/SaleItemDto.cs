namespace DailyMart.Application.Sales;

public class SaleItemDto
{
    public long Id { get; init; }

    public long ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal UnitCost { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal LineTotal { get; init; }
}
