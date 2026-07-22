using DailyMart.Domain.Common;

namespace DailyMart.Domain.Suppliers;

/// <summary>
/// OpeningBalance is write-once (set at creation, when the one matching OpeningBalance ledger entry is
/// also created) - see Module 5 Step 1's scope decision. CurrentDue is a cached running balance, always
/// kept in lockstep with SupplierLedgerEntry rows by SupplierService - never assigned anywhere else.
/// </summary>
public class Supplier : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal CurrentDue { get; set; }
}
