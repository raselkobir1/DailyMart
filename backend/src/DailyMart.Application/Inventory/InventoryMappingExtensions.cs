using DailyMart.Domain.Inventory;

namespace DailyMart.Application.Inventory;

internal static class InventoryMappingExtensions
{
    public static InventoryTransactionDto ToDto(this InventoryTransaction transaction, InventoryLookups lookups)
    {
        var (name, code) = lookups.ProductInfo.GetValueOrDefault(transaction.ProductId, (string.Empty, string.Empty));

        return new InventoryTransactionDto
        {
            Id = transaction.Id,
            ProductId = transaction.ProductId,
            ProductName = name,
            ProductCode = code,
            TransactionType = transaction.TransactionType.ToString(),
            QuantityChange = transaction.QuantityChange,
            BalanceAfter = transaction.BalanceAfter,
            ReferenceType = transaction.ReferenceType,
            ReferenceId = transaction.ReferenceId,
            Notes = transaction.Notes,
            TransactionDate = transaction.TransactionDate
        };
    }

    public static InventoryAdjustmentDto ToDto(this InventoryAdjustment adjustment, InventoryLookups lookups)
    {
        var (name, code) = lookups.ProductInfo.GetValueOrDefault(adjustment.ProductId, (string.Empty, string.Empty));

        return new InventoryAdjustmentDto
        {
            Id = adjustment.Id,
            ProductId = adjustment.ProductId,
            ProductName = name,
            ProductCode = code,
            AdjustmentType = adjustment.AdjustmentType.ToString(),
            QuantityChange = adjustment.QuantityChange,
            Reason = adjustment.Reason,
            AdjustmentDate = adjustment.AdjustmentDate
        };
    }
}
