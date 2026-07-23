namespace DailyMart.Application.Sales;

public class SaleReturnItemRequestDto
{
    public long SaleItemId { get; init; }

    /// <summary>UnitPrice/LineTotal aren't accepted here - SaleReturnService copies UnitPrice from the
    /// original sale line and computes LineTotal itself.</summary>
    public decimal Quantity { get; init; }
}
