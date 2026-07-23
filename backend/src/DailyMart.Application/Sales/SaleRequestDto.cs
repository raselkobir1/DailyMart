using DailyMart.Domain.Common;

namespace DailyMart.Application.Sales;

/// <summary>Create-only - see ISaleService's doc comment for why there's no matching UpdateAsync. CustomerId
/// is optional for a Cash sale (a walk-in with no account); SaleService requires it for Credit/Partial.</summary>
public class SaleRequestDto
{
    public long? CustomerId { get; init; }

    public DateTimeOffset SaleDate { get; init; }

    public PaymentType PaymentType { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal VatAmount { get; init; }

    /// <summary>Only meaningful when PaymentType is Partial - SaleService derives Cash/Credit's PaidAmount
    /// from the total instead of trusting this field verbatim.</summary>
    public decimal PaidAmount { get; init; }

    public string? Notes { get; init; }

    public List<SaleItemRequestDto> Items { get; init; } = [];
}
