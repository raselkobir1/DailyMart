namespace DailyMart.Application.Purchases;

public class PurchaseReturnItemRequestDto
{
    public long PurchaseItemId { get; init; }

    /// <summary>UnitPrice/LineTotal aren't accepted here - PurchaseReturnService copies UnitPrice from
    /// the original purchase line and computes LineTotal itself.</summary>
    public decimal Quantity { get; init; }
}
