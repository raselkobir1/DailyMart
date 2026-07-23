using DailyMart.Application.Suppliers;

namespace DailyMart.UnitTests.Suppliers;

public class PaySupplierRequestValidatorTests
{
    private readonly PaySupplierRequestValidator _validator = new();

    [Fact]
    public void A_positive_amount_is_valid()
    {
        Assert.True(_validator.Validate(new PaySupplierRequestDto { Amount = 50 }).IsValid);
    }

    [Fact]
    public void A_zero_amount_is_invalid()
    {
        Assert.False(_validator.Validate(new PaySupplierRequestDto { Amount = 0 }).IsValid);
    }

    [Fact]
    public void A_negative_amount_is_invalid()
    {
        Assert.False(_validator.Validate(new PaySupplierRequestDto { Amount = -10 }).IsValid);
    }
}
