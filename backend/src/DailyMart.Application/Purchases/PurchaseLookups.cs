namespace DailyMart.Application.Purchases;

/// <summary>Batched id-to-name lookups so PurchaseDto/PurchaseItemDto can surface readable names without
/// an EF navigation property - same pattern as Products/ProductLookups.cs.</summary>
internal sealed record PurchaseLookups(
    Dictionary<long, string> SupplierNames,
    Dictionary<long, (string Name, string Code)> ProductInfo);
