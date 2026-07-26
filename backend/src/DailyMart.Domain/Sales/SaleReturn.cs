using DailyMart.Domain.Common;

namespace DailyMart.Domain.Sales;

/// <summary>Mirrors PurchaseReturn - one row per sale-return document. No stored return number, computed
/// from Id the same way (e.g. "SRET-000001").</summary>
public class SaleReturn : TenantOwnedEntity
{
    public long SaleId { get; set; }

    public DateTimeOffset ReturnDate { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>The portion of TotalAmount actually refunded in cash, as opposed to reducing the
    /// customer's outstanding due - proportional to how much of the original sale was actually paid (see
    /// SaleReturnService.CreateAsync). A full-Cash sale's return refunds 100% in cash; a full-Credit
    /// sale's return refunds none (the customer never paid anything to refund); a Partial sale's return
    /// splits proportionally, matching CLAUDE.md §8's "partial payment updates both cash and due
    /// simultaneously" applied symmetrically to its return.</summary>
    public decimal RefundAmount { get; set; }

    public string? Notes { get; set; }
}
