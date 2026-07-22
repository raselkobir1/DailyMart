using DailyMart.Domain.Common;

namespace DailyMart.Application.Purchases;

/// <summary>One shape for both create and update - like Customer (Module 6), there's no field only one
/// operation can set.</summary>
public class PurchaseRequestDto
{
    public long SupplierId { get; init; }

    public DateTimeOffset PurchaseDate { get; init; }

    public PaymentType PaymentType { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal VatAmount { get; init; }

    /// <summary>Only meaningful when PaymentType is Partial - PurchaseService derives Cash/Credit's
    /// PaidAmount from the total instead of trusting this field verbatim.</summary>
    public decimal PaidAmount { get; init; }

    public string? Notes { get; init; }

    public List<PurchaseItemRequestDto> Items { get; init; } = [];
}
