namespace DailyMart.Application.Sales;

/// <summary>SaleId comes from the route (nested under /api/sales/{saleId}/returns), not this body.</summary>
public class SaleReturnRequestDto
{
    public DateTimeOffset ReturnDate { get; init; }

    public string? Notes { get; init; }

    public List<SaleReturnItemRequestDto> Items { get; init; } = [];
}
