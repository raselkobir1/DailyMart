namespace DailyMart.Domain.Inventory;

/// <summary>
/// The full vocabulary is defined now even though this module (Purchase) only ever produces
/// Purchase/PurchaseReturn - Sale/SaleReturn (Module 9) and Adjustment/Damaged (Module 8) reuse this
/// same table rather than each needing their own, mirroring how SupplierLedgerEntryType (Module 5) was
/// defined in full before Purchase existed to write most of its values.
/// </summary>
public enum InventoryTransactionType
{
    Purchase,
    PurchaseReturn,
    Sale,
    SaleReturn,
    Adjustment,
    Damaged
}
