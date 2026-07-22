using DailyMart.Domain.Common;

namespace DailyMart.Domain.Purchases;

public class PurchaseItem : AuditableEntity
{
    public long PurchaseId { get; set; }

    public long ProductId { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>The price paid this time - may differ from Product.PurchasePrice.</summary>
    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }
}
