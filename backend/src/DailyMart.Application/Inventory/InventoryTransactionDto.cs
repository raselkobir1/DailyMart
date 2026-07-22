namespace DailyMart.Application.Inventory;

public class InventoryTransactionDto
{
    public long Id { get; init; }

    public long ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string ProductCode { get; init; } = string.Empty;

    public string TransactionType { get; init; } = string.Empty;

    public decimal QuantityChange { get; init; }

    public decimal BalanceAfter { get; init; }

    public string ReferenceType { get; init; } = string.Empty;

    public long ReferenceId { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset TransactionDate { get; init; }
}
