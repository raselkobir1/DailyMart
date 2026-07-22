namespace DailyMart.Application.Customers;

/// <summary>Used for both create and update - unlike Product/Supplier, there's no field only one of the
/// two operations can set (no opening balance/stock equivalent here), so the shape is identical either
/// way - same reasoning as Module 3's master data DTOs.</summary>
public class CustomerRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }
}
