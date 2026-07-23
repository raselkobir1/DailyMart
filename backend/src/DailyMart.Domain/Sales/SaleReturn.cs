using DailyMart.Domain.Common;

namespace DailyMart.Domain.Sales;

/// <summary>Mirrors PurchaseReturn - one row per sale-return document. No stored return number, computed
/// from Id the same way (e.g. "SRET-000001").</summary>
public class SaleReturn : AuditableEntity
{
    public long SaleId { get; set; }

    public DateTimeOffset ReturnDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
}
