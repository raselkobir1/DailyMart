using DailyMart.Domain.Common;

namespace DailyMart.Domain.Purchases;

public class PurchaseReturnItem : AuditableEntity
{
    public long PurchaseReturnId { get; set; }

    /// <summary>Which original PurchaseItem this return is against - returned quantity is validated
    /// against that line's quantity minus whatever's already been returned from it.</summary>
    public long PurchaseItemId { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Copied from the original line at return time.</summary>
    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
