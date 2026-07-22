namespace DailyMart.Application.Purchases;

public class PurchaseItemRequestDto
{
    public long ProductId { get; init; }

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal DiscountAmount { get; init; }
}
