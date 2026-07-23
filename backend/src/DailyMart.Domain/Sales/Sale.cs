using DailyMart.Domain.Common;

namespace DailyMart.Domain.Sales;

/// <summary>
/// A POS sale header. No stored "sale number" - computed from Id at DTO-mapping time (e.g. "SALE-000001"),
/// same reasoning as Purchase (Module 7). CustomerId is nullable - a walk-in Cash sale needs no customer,
/// but Credit/Partial sales require one (enforced in SaleService, not here) since they create a due that
/// must be collectible from someone. Create + read only - see ISaleService's doc comment for why there's
/// no Update/Delete, unlike Purchase.
/// </summary>
public class Sale : AuditableEntity
{
    public long? CustomerId { get; set; }

    public DateTimeOffset SaleDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal VatAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    /// <summary>Sum of each item's Quantity * UnitCost - the cost-of-goods-sold snapshot this sale was
    /// posted against, so profit can be reported without re-joining historical purchase data later.</summary>
    public decimal TotalCost { get; set; }

    /// <summary>TotalAmount - TotalCost. An MVP-level gross profit figure at the sale level; Module 13
    /// (Profit &amp; Loss) does the fuller period-level accounting.</summary>
    public decimal ProfitAmount { get; set; }

    public string? Notes { get; set; }
}
