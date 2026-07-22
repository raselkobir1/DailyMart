using DailyMart.Domain.Purchases;

namespace DailyMart.Application.Purchases;

internal static class PurchaseReturnMappingExtensions
{
    public static PurchaseReturn ToEntity(this PurchaseReturnRequestDto request, long purchaseId) => new()
    {
        PurchaseId = purchaseId,
        ReturnDate = request.ReturnDate,
        Notes = request.Notes
    };

    public static List<PurchaseReturnItem> ToEntities(this IEnumerable<PurchaseReturnItemRequestDto> items) =>
        items.Select(i => new PurchaseReturnItem
        {
            PurchaseItemId = i.PurchaseItemId,
            Quantity = i.Quantity
        }).ToList();

    public static PurchaseReturnDto ToDto(
        this PurchaseReturn purchaseReturn, IReadOnlyList<PurchaseReturnItem> items, PurchaseReturnLookups lookups) => new()
    {
        Id = purchaseReturn.Id,
        ReturnNumber = PurchaseNumberFormatter.FormatReturn(purchaseReturn.Id),
        PurchaseId = purchaseReturn.PurchaseId,
        PurchaseNumber = PurchaseNumberFormatter.FormatPurchase(purchaseReturn.PurchaseId),
        ReturnDate = purchaseReturn.ReturnDate,
        TotalAmount = purchaseReturn.TotalAmount,
        Notes = purchaseReturn.Notes,
        Items = items.Select(i => i.ToDto(lookups)).ToList()
    };

    public static PurchaseReturnItemDto ToDto(this PurchaseReturnItem item, PurchaseReturnLookups lookups)
    {
        var (productId, productName) = lookups.ProductByPurchaseItem
            .GetValueOrDefault(item.PurchaseItemId, (0, string.Empty));

        return new PurchaseReturnItemDto
        {
            Id = item.Id,
            PurchaseItemId = item.PurchaseItemId,
            ProductId = productId,
            ProductName = productName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.LineTotal
        };
    }
}
