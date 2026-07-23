namespace DailyMart.Application.Rbac;

/// <summary>The update shape - no Key (write-once, only creation can set it; see Menu's doc comment).
/// CreateMenuRequestDto extends this with the one field only creation needs.</summary>
public class MenuRequestDto
{
    public string Label { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public long? ParentId { get; init; }
}
