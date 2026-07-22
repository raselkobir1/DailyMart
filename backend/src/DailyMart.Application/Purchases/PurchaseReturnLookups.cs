namespace DailyMart.Application.Purchases;

/// <summary>Maps a PurchaseReturnItem's PurchaseItemId to the product it was originally purchased against
/// - PurchaseReturnItem itself has no ProductId column.</summary>
internal sealed record PurchaseReturnLookups(
    Dictionary<long, (long ProductId, string ProductName)> ProductByPurchaseItem);
