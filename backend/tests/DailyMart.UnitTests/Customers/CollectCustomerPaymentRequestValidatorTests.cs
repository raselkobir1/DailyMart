using DailyMart.Application.Customers;

namespace DailyMart.UnitTests.Customers;

public class CollectCustomerPaymentRequestValidatorTests
{
    private readonly CollectCustomerPaymentRequestValidator _validator = new();

    [Fact]
    public void A_positive_amount_is_valid()
    {
        Assert.True(_validator.Validate(new CollectCustomerPaymentRequestDto { Amount = 50 }).IsValid);
    }

    [Fact]
    public void A_zero_amount_is_invalid()
    {
        Assert.False(_validator.Validate(new CollectCustomerPaymentRequestDto { Amount = 0 }).IsValid);
    }

    [Fact]
    public void A_negative_amount_is_invalid()
    {
        Assert.False(_validator.Validate(new CollectCustomerPaymentRequestDto { Amount = -10 }).IsValid);
    }
}
