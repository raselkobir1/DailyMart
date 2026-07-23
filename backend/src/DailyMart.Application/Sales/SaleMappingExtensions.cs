using DailyMart.Domain.Sales;

namespace DailyMart.Application.Sales;

internal static class SaleMappingExtensions
{
    public static Sale ToEntity(this SaleRequestDto request) => new()
    {
        CustomerId = request.CustomerId,
        SaleDate = request.SaleDate,
        PaymentType = request.PaymentType,
        DiscountAmount = request.DiscountAmount,
        VatAmount = request.VatAmount,
        PaidAmount = request.PaidAmount,
        Notes = request.Notes
    };

    public static List<SaleItem> ToEntities(this IEnumerable<SaleItemRequestDto> items) =>
        items.Select(i => new SaleItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountAmount = i.DiscountAmount
        }).ToList();

    public static SaleDto ToDto(this Sale sale, IReadOnlyList<SaleItem> items, SaleLookups lookups) => new()
    {
        Id = sale.Id,
        SaleNumber = SaleNumberFormatter.FormatSale(sale.Id),
        CustomerId = sale.CustomerId,
        CustomerName = sale.CustomerId is null
            ? null
            : lookups.CustomerNames.GetValueOrDefault(sale.CustomerId.Value, string.Empty),
        SaleDate = sale.SaleDate,
        PaymentType = sale.PaymentType.ToString(),
        SubtotalAmount = sale.SubtotalAmount,
        DiscountAmount = sale.DiscountAmount,
        VatAmount = sale.VatAmount,
        TotalAmount = sale.TotalAmount,
        PaidAmount = sale.PaidAmount,
        DueAmount = sale.DueAmount,
        TotalCost = sale.TotalCost,
        ProfitAmount = sale.ProfitAmount,
        Notes = sale.Notes,
        Items = items.Select(i => i.ToDto(lookups)).ToList()
    };

    public static SaleItemDto ToDto(this SaleItem item, SaleLookups lookups)
    {
        var (name, code) = lookups.ProductInfo.GetValueOrDefault(item.ProductId, (string.Empty, string.Empty));

        return new SaleItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = name,
            ProductCode = code,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            UnitCost = item.UnitCost,
            DiscountAmount = item.DiscountAmount,
            LineTotal = item.LineTotal
        };
    }
}
