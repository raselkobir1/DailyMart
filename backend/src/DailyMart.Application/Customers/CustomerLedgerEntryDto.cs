namespace DailyMart.Application.Customers;

public class CustomerLedgerEntryDto
{
    public long Id { get; init; }

    public long CustomerId { get; init; }

    public string EntryType { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Amount { get; init; }

    public decimal BalanceAfter { get; init; }

    public DateTimeOffset TransactionDate { get; init; }
}
