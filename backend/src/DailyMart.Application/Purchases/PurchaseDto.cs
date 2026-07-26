namespace DailyMart.Application.Purchases;

public class PurchaseDto
{
    public long Id { get; init; }

    /// <summary>Computed from Id, never stored - see Purchase's doc comment.</summary>
    public string PurchaseNumber { get; init; } = string.Empty;

    public long SupplierId { get; init; }

    public string SupplierName { get; init; } = string.Empty;

    public DateTimeOffset PurchaseDate { get; init; }

    public string PaymentType { get; init; } = string.Empty;

    public decimal SubtotalAmount { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal VatAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal PaidAmount { get; init; }

    public decimal DueAmount { get; init; }

    public string? Notes { get; init; }

    /// <summary>Number of PurchaseReturn transactions recorded against this purchase (not returned line-item
    /// count) - lets the purchase list flag "this purchase has returns" without a separate call per row.</summary>
    public int ReturnCount { get; init; }

    public List<PurchaseItemDto> Items { get; init; } = [];
}
