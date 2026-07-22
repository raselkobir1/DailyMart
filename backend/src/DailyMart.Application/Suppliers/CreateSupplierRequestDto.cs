namespace DailyMart.Application.Suppliers;

/// <summary>Adds OpeningBalance - write-once, only settable here (same inheritance pattern as Module 4's
/// CreateProductRequestDto, for the same reason: a genuine strict superset of the update shape).</summary>
public class CreateSupplierRequestDto : SupplierRequestDto
{
    public decimal OpeningBalance { get; init; }
}
