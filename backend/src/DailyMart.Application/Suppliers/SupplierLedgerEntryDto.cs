namespace DailyMart.Application.Suppliers;

public class SupplierLedgerEntryDto
{
    public long Id { get; init; }

    public long SupplierId { get; init; }

    public string EntryType { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Amount { get; init; }

    public decimal BalanceAfter { get; init; }

    public DateTimeOffset TransactionDate { get; init; }
}
