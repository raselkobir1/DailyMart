namespace DailyMart.Application.Products;

/// <summary>
/// The update shape - deliberately has no CurrentStock (see Module 4 Step 1's scope decision).
/// CreateProductRequestDto extends this with the one field creation needs that update doesn't.
/// </summary>
public class ProductRequestDto
{
    public string Code { get; init; } = string.Empty;

    /// <summary>Null/empty means "generate one" on create, or "leave unchanged" on update.</summary>
    public string? Barcode { get; init; }

    public string Name { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public long? BrandId { get; init; }

    public long UnitId { get; init; }

    public decimal PurchasePrice { get; init; }

    public decimal SellingPrice { get; init; }

    public decimal? WholesalePrice { get; init; }

    public decimal DiscountPercentage { get; init; }

    public decimal TaxPercentage { get; init; }

    public decimal MinimumStock { get; init; }

    public bool AllowPriceBelowCost { get; init; }
}
