using DailyMart.Domain.Common;

namespace DailyMart.Domain.Customers;

/// <summary>
/// One row per balance-affecting event for a customer - mirrors SupplierLedgerEntry exactly. Due is a sum
/// of transactions, never a bare editable field, so it can always be reconciled against Sale/SaleReturn data.
/// </summary>
public class CustomerLedgerEntry : AuditableEntity
{
    public long CustomerId { get; set; }

    public CustomerLedgerEntryType EntryType { get; set; }

    public string? Description { get; set; }

    /// <summary>Signed: positive increases what the customer owes, negative decreases it. May differ from
    /// the amount requested by the caller - see ICustomerService.AdjustDueAsync's negative-due clamp.</summary>
    public decimal Amount { get; set; }

    /// <summary>Running-balance snapshot immediately after this entry - standard ledger UX.</summary>
    public decimal BalanceAfter { get; set; }

    public DateTimeOffset TransactionDate { get; set; }
}
