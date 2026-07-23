namespace DailyMart.Application.Expenses;

public class ExpenseDto
{
    public long Id { get; init; }

    public string Category { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset ExpenseDate { get; init; }
}
