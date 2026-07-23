using DailyMart.Domain.Common;

namespace DailyMart.Domain.Customers;

/// <summary>
/// CurrentDue/ledger added in Module 9 (POS Sales) - see Module 6 Step 1's original scope decision for why
/// it wasn't here from the start. No OpeningBalance counterpart to Supplier's: customers aren't onboarded
/// with pre-existing debt in this MVP, so CurrentDue always starts at zero and only ever moves via
/// ICustomerService.AdjustDueAsync (credit sales, sale returns, and eventually Module 10's payments).
/// </summary>
public class Customer : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique when provided - the stronger identity signal; Name deliberately isn't unique.</summary>
    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public decimal CurrentDue { get; set; }
}
