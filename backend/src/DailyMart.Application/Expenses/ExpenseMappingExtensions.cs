using DailyMart.Domain.Expenses;

namespace DailyMart.Application.Expenses;

internal static class ExpenseMappingExtensions
{
    public static ExpenseDto ToDto(this Expense expense) => new()
    {
        Id = expense.Id,
        Category = expense.Category.ToString(),
        Amount = expense.Amount,
        Description = expense.Description,
        ExpenseDate = expense.ExpenseDate
    };

    public static Expense ToEntity(this ExpenseRequestDto request) => new()
    {
        Category = request.Category,
        Amount = request.Amount,
        Description = request.Description,
        ExpenseDate = request.ExpenseDate
    };

    public static void ApplyTo(this ExpenseRequestDto request, Expense expense)
    {
        expense.Category = request.Category;
        expense.Amount = request.Amount;
        expense.Description = request.Description;
        expense.ExpenseDate = request.ExpenseDate;
    }
}
