using DailyMart.Application.Suppliers;

namespace DailyMart.UnitTests.Suppliers;

public class SupplierRequestValidatorTests
{
    private readonly SupplierRequestValidator _validator = new();

    private static SupplierRequestDto ValidRequest(string name = "Acme Distributors", string? email = null) => new()
    {
        Name = name,
        Email = email
    };

    [Fact]
    public void A_valid_request_passes()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void Empty_name_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(name: "")).IsValid);
    }

    [Fact]
    public void A_malformed_email_is_invalid_when_provided()
    {
        Assert.False(_validator.Validate(ValidRequest(email: "not-an-email")).IsValid);
    }

    [Fact]
    public void A_missing_email_is_valid_since_its_optional()
    {
        Assert.True(_validator.Validate(ValidRequest(email: null)).IsValid);
    }
}
