namespace DailyMart.Application.Purchases;

/// <summary>PurchaseId comes from the route (nested under /api/purchases/{purchaseId}/returns), not this
/// body.</summary>
public class PurchaseReturnRequestDto
{
    public DateTimeOffset ReturnDate { get; init; }

    public string? Notes { get; init; }

    public List<PurchaseReturnItemRequestDto> Items { get; init; } = [];
}
