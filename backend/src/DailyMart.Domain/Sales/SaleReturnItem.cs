using DailyMart.Domain.Common;

namespace DailyMart.Domain.Sales;

public class SaleReturnItem : AuditableEntity
{
    public long SaleReturnId { get; set; }

    /// <summary>Which original SaleItem this return is against - returned quantity is validated against
    /// that line's quantity minus whatever's already been returned from it (mirrors PurchaseReturnItem).</summary>
    public long SaleItemId { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Copied from the original line at return time.</summary>
    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
