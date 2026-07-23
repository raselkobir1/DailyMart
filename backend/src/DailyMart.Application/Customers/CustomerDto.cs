namespace DailyMart.Application.Customers;

public class CustomerDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public decimal CurrentDue { get; init; }
}
