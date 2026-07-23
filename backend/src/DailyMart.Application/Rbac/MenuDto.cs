namespace DailyMart.Application.Rbac;

public class MenuDto
{
    public long Id { get; init; }

    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public string Icon { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public long? ParentId { get; init; }
}
