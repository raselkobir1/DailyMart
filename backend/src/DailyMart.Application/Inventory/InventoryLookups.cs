namespace DailyMart.Application.Inventory;

/// <summary>Batched product id-to-(name, code) lookup so InventoryTransactionDto/InventoryAdjustmentDto
/// can surface readable names without an EF navigation property - same pattern as
/// Products/ProductLookups.cs and Purchases/PurchaseLookups.cs.</summary>
internal sealed record InventoryLookups(Dictionary<long, (string Name, string Code)> ProductInfo);
