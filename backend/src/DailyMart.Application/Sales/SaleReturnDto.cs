namespace DailyMart.Application.Sales;

public class SaleReturnDto
{
    public long Id { get; init; }

    /// <summary>Computed from Id, never stored - same as SaleDto.SaleNumber.</summary>
    public string ReturnNumber { get; init; } = string.Empty;

    public long SaleId { get; init; }

    public string SaleNumber { get; init; } = string.Empty;

    public DateTimeOffset ReturnDate { get; init; }

    public decimal TotalAmount { get; init; }

    /// <summary>The portion of TotalAmount refunded in cash rather than deducted from the customer's due -
    /// see SaleReturn's doc comment.</summary>
    public decimal RefundAmount { get; init; }

    public string? Notes { get; init; }

    public List<SaleReturnItemDto> Items { get; init; } = [];
}
