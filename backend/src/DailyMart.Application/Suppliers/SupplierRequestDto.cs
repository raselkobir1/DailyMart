namespace DailyMart.Application.Suppliers;

/// <summary>The update shape - deliberately has no OpeningBalance/CurrentDue (Module 5 Step 1's scope
/// decision). CreateSupplierRequestDto extends this with the one field only creation can set.</summary>
public class SupplierRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? ContactPerson { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Address { get; init; }
}
