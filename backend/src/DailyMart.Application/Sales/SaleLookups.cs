namespace DailyMart.Application.Sales;

/// <summary>Batched id-to-name lookups so SaleDto/SaleItemDto can surface readable names without an EF
/// navigation property - same pattern as Purchases/PurchaseLookups.cs.</summary>
internal sealed record SaleLookups(
    Dictionary<long, string> CustomerNames,
    Dictionary<long, (string Name, string Code)> ProductInfo);
