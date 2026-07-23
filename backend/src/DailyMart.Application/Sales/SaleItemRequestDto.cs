namespace DailyMart.Application.Sales;

public class SaleItemRequestDto
{
    public long ProductId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal DiscountAmount { get; init; }
}
