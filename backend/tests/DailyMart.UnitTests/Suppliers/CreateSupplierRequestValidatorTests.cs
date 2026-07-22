using DailyMart.Application.Suppliers;

namespace DailyMart.UnitTests.Suppliers;

public class CreateSupplierRequestValidatorTests
{
    private readonly CreateSupplierRequestValidator _validator = new();

    private static CreateSupplierRequestDto ValidRequest(string name = "Acme Distributors", decimal openingBalance = 0) => new()
    {
        Name = name,
        OpeningBalance = openingBalance
    };

    [Fact]
    public void A_valid_request_passes()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void A_negative_OpeningBalance_is_valid_the_supplier_may_legitimately_be_owed_money()
    {
        Assert.True(_validator.Validate(ValidRequest(openingBalance: -500)).IsValid);
    }

    [Fact]
    public void Included_SupplierRequestValidator_rules_still_apply()
    {
        // Proves Include() actually pulled in the base rules, not just that this validator has its own.
        Assert.False(_validator.Validate(ValidRequest(name: "")).IsValid);
    }
}
