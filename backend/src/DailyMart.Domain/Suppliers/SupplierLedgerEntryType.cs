namespace DailyMart.Domain.Suppliers;

public enum SupplierLedgerEntryType
{
    /// <summary>Written only by Module 5 (Supplier), at creation.</summary>
    OpeningBalance,

    /// <summary>Written by Module 7 (Purchase) - both when a credit/partial purchase is posted and
    /// (as a reversal-then-reapply pair) when one is later updated.</summary>
    Purchase,

    /// <summary>Reserved for Module 11 (Supplier Due) - not written by any module yet.</summary>
    Payment,

    /// <summary>Reserved for manual corrections - no module writes this yet (Module 5's scope decision:
    /// no "add adjustment" endpoint exists).</summary>
    Adjustment,

    /// <summary>Written by Module 7 (Purchase) when goods are returned to a supplier - kept distinct
    /// from the generic Adjustment so the ledger reads clearly.</summary>
    PurchaseReturn
}
