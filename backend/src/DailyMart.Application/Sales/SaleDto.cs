namespace DailyMart.Application.Sales;

public class SaleDto
{
    public long Id { get; init; }

    /// <summary>Computed from Id, never stored - see Sale's doc comment.</summary>
    public string SaleNumber { get; init; } = string.Empty;

    public long? CustomerId { get; init; }

    /// <summary>Empty for a walk-in Cash sale with no customer.</summary>
    public string? CustomerName { get; init; }

    public DateTimeOffset SaleDate { get; init; }

    public string PaymentType { get; init; } = string.Empty;

    public decimal SubtotalAmount { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal VatAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal PaidAmount { get; init; }

    public decimal DueAmount { get; init; }

    public decimal TotalCost { get; init; }

    public decimal ProfitAmount { get; init; }

    public string? Notes { get; init; }

    public List<SaleItemDto> Items { get; init; } = [];
}
