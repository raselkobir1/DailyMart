namespace DailyMart.Application.Rbac;

/// <summary>Used for both create and update - IsSystem/IsDefault are never caller-settable (seed-only
/// concepts), so there's no separate Create shape the way Product/Supplier need one.</summary>
public class RoleRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
