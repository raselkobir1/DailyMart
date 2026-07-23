namespace DailyMart.Domain.Expenses;

/// <summary>The fixed category set from the BRD - a plain enum rather than a manageable master-data
/// table like Category/Brand/Unit, since these five are the expense types the BRD calls for, not an
/// open-ended list a shop owner is expected to grow.</summary>
public enum ExpenseCategory
{
    Rent,
    Salary,
    Electricity,
    Internet,
    Miscellaneous
}
