namespace DailyMart.Application.Products;

/// <summary>
/// Product has no EF navigation properties to Category/Brand/Unit (deliberately, per Step 2), so a list
/// of products can't just ".Include()" its way to their names for display. This batches one lookup query
/// per referenced table across a whole page of products, instead of querying per-row.
/// </summary>
internal sealed record ProductLookups(
    Dictionary<long, string> CategoryNames,
    Dictionary<long, string> BrandNames,
    Dictionary<long, (string Name, string Symbol)> Units);
