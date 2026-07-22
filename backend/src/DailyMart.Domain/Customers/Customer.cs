using DailyMart.Domain.Common;

namespace DailyMart.Domain.Customers;

/// <summary>
/// No CurrentDue/ledger here - see Module 6 Step 1's scope decision. A customer only ever owes anything
/// once Sale (Module 9) creates a credit-sale ledger entry, which doesn't exist yet.
/// </summary>
public class Customer : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique when provided - the stronger identity signal; Name deliberately isn't unique.</summary>
    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }
}
