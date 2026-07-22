namespace DailyMart.Application.Products;

public class ProductDto
{
    public long Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Barcode { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public long CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public long? BrandId { get; init; }

    public string? BrandName { get; init; }

    public long UnitId { get; init; }

    public string UnitName { get; init; } = string.Empty;

    public string UnitSymbol { get; init; } = string.Empty;

    public decimal PurchasePrice { get; init; }

    public decimal SellingPrice { get; init; }

    public decimal? WholesalePrice { get; init; }

    public decimal DiscountPercentage { get; init; }

    public decimal TaxPercentage { get; init; }

    public decimal CurrentStock { get; init; }

    public decimal MinimumStock { get; init; }

    public bool AllowPriceBelowCost { get; init; }

    public string? ImageUrl { get; init; }
}
