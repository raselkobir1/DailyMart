using DailyMart.Domain.Common;

namespace DailyMart.Domain.Suppliers;

/// <summary>
/// One row per balance-affecting event for a supplier - the running-balance ledger CLAUDE.md's business
/// rule ("supplier due must always match unpaid purchases") implies: due is a sum of transactions, never
/// a bare editable field.
/// </summary>
public class SupplierLedgerEntry : AuditableEntity
{
    public long SupplierId { get; set; }

    public SupplierLedgerEntryType EntryType { get; set; }

    public string? Description { get; set; }

    /// <summary>Signed: positive increases what's owed to the supplier, negative decreases it.</summary>
    public decimal Amount { get; set; }

    /// <summary>Running-balance snapshot immediately after this entry - standard ledger UX.</summary>
    public decimal BalanceAfter { get; set; }

    public DateTimeOffset TransactionDate { get; set; }
}
