using DailyMart.Domain.Sales;

namespace DailyMart.Application.Sales;

internal static class SaleReturnMappingExtensions
{
    public static SaleReturn ToEntity(this SaleReturnRequestDto request, long saleId) => new()
    {
        SaleId = saleId,
        ReturnDate = request.ReturnDate,
        Notes = request.Notes
    };

    public static List<SaleReturnItem> ToEntities(this IEnumerable<SaleReturnItemRequestDto> items) =>
        items.Select(i => new SaleReturnItem
        {
            SaleItemId = i.SaleItemId,
            Quantity = i.Quantity
        }).ToList();

    public static SaleReturnDto ToDto(
        this SaleReturn saleReturn, IReadOnlyList<SaleReturnItem> items, SaleReturnLookups lookups) => new()
    {
        Id = saleReturn.Id,
        ReturnNumber = SaleNumberFormatter.FormatReturn(saleReturn.Id),
        SaleId = saleReturn.SaleId,
        SaleNumber = SaleNumberFormatter.FormatSale(saleReturn.SaleId),
        ReturnDate = saleReturn.ReturnDate,
        TotalAmount = saleReturn.TotalAmount,
        Notes = saleReturn.Notes,
        Items = items.Select(i => i.ToDto(lookups)).ToList()
    };

    public static SaleReturnItemDto ToDto(this SaleReturnItem item, SaleReturnLookups lookups)
    {
        var (productId, productName) = lookups.ProductBySaleItem
            .GetValueOrDefault(item.SaleItemId, (0, string.Empty));

        return new SaleReturnItemDto
        {
            Id = item.Id,
            SaleItemId = item.SaleItemId,
            ProductId = productId,
            ProductName = productName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.LineTotal
        };
    }
}
