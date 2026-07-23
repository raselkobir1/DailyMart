namespace DailyMart.Application.Sales;

/// <summary>Maps a SaleItemId to its ProductId/ProductName - mirrors Purchases/PurchaseReturnLookups.cs.</summary>
internal sealed record SaleReturnLookups(Dictionary<long, (long ProductId, string ProductName)> ProductBySaleItem);
