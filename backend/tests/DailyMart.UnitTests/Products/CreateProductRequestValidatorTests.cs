using DailyMart.Application.Products;

namespace DailyMart.UnitTests.Products;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    private static CreateProductRequestDto ValidRequest(string code = "P-001", decimal currentStock = 0) => new()
    {
        Code = code,
        Name = "Rice 1kg",
        CategoryId = 1,
        UnitId = 1,
        PurchasePrice = 50,
        SellingPrice = 60,
        CurrentStock = currentStock
    };

    [Fact]
    public void A_valid_request_passes()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void A_negative_CurrentStock_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(currentStock: -1)).IsValid);
    }

    [Fact]
    public void Included_ProductRequestValidator_rules_still_apply()
    {
        // Proves Include() actually pulled in the base rules, not just that this validator has its own.
        Assert.False(_validator.Validate(ValidRequest(code: "")).IsValid);
    }
}
