using FluentValidation;

namespace DailyMart.Application.Expenses;

public class ExpenseRequestValidator : AbstractValidator<ExpenseRequestDto>
{
    public ExpenseRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.ExpenseDate).NotEmpty();
    }
}
