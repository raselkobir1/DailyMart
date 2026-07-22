namespace DailyMart.Application.Purchases;

public class PurchaseReturnItemDto
{
    public long Id { get; init; }

    public long PurchaseItemId { get; init; }

    /// <summary>Denormalized from the original PurchaseItem - PurchaseReturnItem itself has no ProductId
    /// column.</summary>
    public long ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal { get; init; }
}
