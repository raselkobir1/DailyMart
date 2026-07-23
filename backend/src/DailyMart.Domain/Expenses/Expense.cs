using DailyMart.Domain.Common;

namespace DailyMart.Domain.Expenses;

/// <summary>
/// A single logged operating cost. Full CRUD (unlike Sale) - an expense is a standalone record with no
/// cascading stock/due effects, so correcting or removing one is a plain field/row edit, not a
/// reverse-and-reapply.
/// </summary>
public class Expense : AuditableEntity
{
    public ExpenseCategory Category { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset ExpenseDate { get; set; }
}
