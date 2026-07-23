using DailyMart.Application.Expenses;
using DailyMart.Domain.Expenses;

namespace DailyMart.UnitTests.Expenses;

public class ExpenseRequestValidatorTests
{
    private readonly ExpenseRequestValidator _validator = new();

    private static ExpenseRequestDto ValidRequest(
        decimal amount = 5000, DateTimeOffset? expenseDate = null) => new()
    {
        Category = ExpenseCategory.Rent,
        Amount = amount,
        Description = "Monthly rent",
        ExpenseDate = expenseDate ?? DateTimeOffset.UtcNow
    };

    [Fact]
    public void A_valid_request_passes()
    {
        Assert.True(_validator.Validate(ValidRequest()).IsValid);
    }

    [Fact]
    public void A_zero_amount_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(amount: 0)).IsValid);
    }

    [Fact]
    public void A_negative_amount_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(amount: -100)).IsValid);
    }

    [Fact]
    public void A_missing_expense_date_is_invalid()
    {
        Assert.False(_validator.Validate(ValidRequest(expenseDate: default(DateTimeOffset))).IsValid);
    }
}
