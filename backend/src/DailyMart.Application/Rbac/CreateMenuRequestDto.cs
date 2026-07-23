namespace DailyMart.Application.Rbac;

/// <summary>Adds Key - write-once, only settable here (same inheritance pattern as Module 4's
/// CreateProductRequestDto).</summary>
public class CreateMenuRequestDto : MenuRequestDto
{
    public string Key { get; init; } = string.Empty;
}
