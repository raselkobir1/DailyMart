using DailyMart.Domain.Products;

namespace DailyMart.Application.Products;

internal static class ProductMappingExtensions
{
    public static Product ToEntity(this CreateProductRequestDto request) => new()
    {
        Code = request.Code,
        Barcode = request.Barcode ?? string.Empty,
        Name = request.Name,
        CategoryId = request.CategoryId,
        BrandId = request.BrandId,
        UnitId = request.UnitId,
        PurchasePrice = request.PurchasePrice,
        SellingPrice = request.SellingPrice,
        WholesalePrice = request.WholesalePrice,
        DiscountPercentage = request.DiscountPercentage,
        TaxPercentage = request.TaxPercentage,
        CurrentStock = request.CurrentStock,
        MinimumStock = request.MinimumStock,
        AllowPriceBelowCost = request.AllowPriceBelowCost
    };

    /// <summary>Doesn't touch Barcode or CurrentStock - the caller (ProductService.UpdateAsync) handles
    /// Barcode itself (it needs a uniqueness check first), and CurrentStock is never editable here at all
    /// (Module 4 Step 1's scope decision).</summary>
    public static void ApplyTo(this ProductRequestDto request, Product product)
    {
        product.Code = request.Code;
        product.Name = request.Name;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.UnitId = request.UnitId;
        product.PurchasePrice = request.PurchasePrice;
        product.SellingPrice = request.SellingPrice;
        product.WholesalePrice = request.WholesalePrice;
        product.DiscountPercentage = request.DiscountPercentage;
        product.TaxPercentage = request.TaxPercentage;
        product.MinimumStock = request.MinimumStock;
        product.AllowPriceBelowCost = request.AllowPriceBelowCost;
    }

    public static ProductDto ToDto(this Product product, ProductLookups lookups)
    {
        var unit = lookups.Units.GetValueOrDefault(product.UnitId);

        return new ProductDto
        {
            Id = product.Id,
            Code = product.Code,
            Barcode = product.Barcode,
            Name = product.Name,
            CategoryId = product.CategoryId,
            CategoryName = lookups.CategoryNames.GetValueOrDefault(product.CategoryId, string.Empty),
            BrandId = product.BrandId,
            BrandName = product.BrandId is { } brandId ? lookups.BrandNames.GetValueOrDefault(brandId) : null,
            UnitId = product.UnitId,
            UnitName = unit.Name ?? string.Empty,
            UnitSymbol = unit.Symbol ?? string.Empty,
            PurchasePrice = product.PurchasePrice,
            SellingPrice = product.SellingPrice,
            WholesalePrice = product.WholesalePrice,
            DiscountPercentage = product.DiscountPercentage,
            TaxPercentage = product.TaxPercentage,
            CurrentStock = product.CurrentStock,
            MinimumStock = product.MinimumStock,
            AllowPriceBelowCost = product.AllowPriceBelowCost,
            ImageUrl = product.ImageUrl
        };
    }
}
