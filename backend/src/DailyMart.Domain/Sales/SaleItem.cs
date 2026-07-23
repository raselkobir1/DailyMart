using DailyMart.Domain.Common;

namespace DailyMart.Domain.Sales;

public class SaleItem : AuditableEntity
{
    public long SaleId { get; set; }

    public long ProductId { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>The price charged this time - defaults from Product.SellingPrice on the frontend, but may
    /// be overridden by the cashier (e.g. a manual discount).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Copied from Product.PurchasePrice at sale time - a cost snapshot so profit can be computed
    /// per line without re-joining historical purchase data later (Purchase's PurchaseItem has no
    /// equivalent since it IS the cost side).</summary>
    public decimal UnitCost { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }
}
