using DailyMart.Application.Products;

namespace DailyMart.UnitTests.Products;

public class ProductRequestValidatorTests
{
    private readonly ProductRequestValidator _validator = new();

    private static ProductRequestDto ValidRequest(
        string code = "P-001",
        long categoryId = 1,
        long unitId = 1,
        decimal purchasePrice = 50,
        decimal sellingPrice = 60,
        decimal discountPercentage = 0,
        decimal taxPercentage = 0,
        decimal minimumStock = 0) => new()
    {
        Code = code,
        Name = "Rice 1kg",
        CategoryId = categoryId,
        UnitId = unitId,
        PurchasePrice = purchasePrice,
        SellingPrice = sellingPrice,
        DiscountPercentage = discountPercentage,
        TaxPercentage = taxPercentage,
        MinimumStock = minimumStock
    };

    [Fact]
    public void A_valid_request_passes()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void Empty_code_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(code: "")).IsValid);
    }

    [Fact]
    public void CategoryId_of_zero_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(categoryId: 0)).IsValid);
    }

    [Fact]
    public void UnitId_of_zero_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(unitId: 0)).IsValid);
    }

    [Fact]
    public void A_negative_purchase_price_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(purchasePrice: -1)).IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void DiscountPercentage_outside_0_to_100_is_invalid(decimal discount)
    {
        Assert.False(_validator.Validate(ValidRequest(discountPercentage: discount)).IsValid);
    }
}
