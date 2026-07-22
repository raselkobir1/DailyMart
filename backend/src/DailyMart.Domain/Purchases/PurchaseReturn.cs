using DailyMart.Domain.Common;

namespace DailyMart.Domain.Purchases;

/// <summary>No stored "return number" - computed from Id at DTO-mapping time, same as Purchase.</summary>
public class PurchaseReturn : AuditableEntity
{
    public long PurchaseId { get; set; }

    public DateTimeOffset ReturnDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
}
