using DailyMart.Domain.Purchases;

namespace DailyMart.Application.Purchases;

internal static class PurchaseMappingExtensions
{
    public static Purchase ToEntity(this PurchaseRequestDto request) => new()
    {
        SupplierId = request.SupplierId,
        PurchaseDate = request.PurchaseDate,
        PaymentType = request.PaymentType,
        DiscountAmount = request.DiscountAmount,
        VatAmount = request.VatAmount,
        PaidAmount = request.PaidAmount,
        Notes = request.Notes
    };

    public static List<PurchaseItem> ToEntities(this IEnumerable<PurchaseItemRequestDto> items) =>
        items.Select(i => new PurchaseItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            DiscountAmount = i.DiscountAmount
        }).ToList();

    public static PurchaseDto ToDto(this Purchase purchase, IReadOnlyList<PurchaseItem> items, PurchaseLookups lookups) => new()
    {
        Id = purchase.Id,
        PurchaseNumber = PurchaseNumberFormatter.FormatPurchase(purchase.Id),
        SupplierId = purchase.SupplierId,
        SupplierName = lookups.SupplierNames.GetValueOrDefault(purchase.SupplierId, string.Empty),
        PurchaseDate = purchase.PurchaseDate,
        PaymentType = purchase.PaymentType.ToString(),
        SubtotalAmount = purchase.SubtotalAmount,
        DiscountAmount = purchase.DiscountAmount,
        VatAmount = purchase.VatAmount,
        TotalAmount = purchase.TotalAmount,
        PaidAmount = purchase.PaidAmount,
        DueAmount = purchase.DueAmount,
        Notes = purchase.Notes,
        Items = items.Select(i => i.ToDto(lookups)).ToList()
    };

    public static PurchaseItemDto ToDto(this PurchaseItem item, PurchaseLookups lookups)
    {
        var (name, code) = lookups.ProductInfo.GetValueOrDefault(item.ProductId, (string.Empty, string.Empty));

        return new PurchaseItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = name,
            ProductCode = code,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.DiscountAmount,
            LineTotal = item.LineTotal
        };
    }
}
