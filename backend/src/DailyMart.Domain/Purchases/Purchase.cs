using DailyMart.Domain.Common;

namespace DailyMart.Domain.Purchases;

/// <summary>
/// No stored "purchase number" - it's computed from Id at DTO-mapping time (e.g. "PUR-000001"), simpler
/// and race-condition-free versus a separately-tracked sequence column.
/// Editing a posted purchase is a reverse-and-reapply, not a field overwrite - see PurchaseService.
/// </summary>
public class Purchase : AuditableEntity
{
    public long SupplierId { get; set; }

    public DateTimeOffset PurchaseDate { get; set; }

    public PaymentType PaymentType { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal VatAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal DueAmount { get; set; }

    public string? Notes { get; set; }
}
