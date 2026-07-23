using DailyMart.Domain.Expenses;

namespace DailyMart.Application.Expenses;

/// <summary>Used for both create and update - the shape is identical either way, same reasoning as
/// CustomerRequestDto (Module 6 Step 6).</summary>
public class ExpenseRequestDto
{
    public ExpenseCategory Category { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset ExpenseDate { get; set; }
}
