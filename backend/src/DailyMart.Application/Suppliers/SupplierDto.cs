namespace DailyMart.Application.Suppliers;

public class SupplierDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }

    public decimal OpeningBalance { get; init; }

    public decimal CurrentDue { get; init; }
}
