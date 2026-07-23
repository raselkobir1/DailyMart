namespace DailyMart.Application.Rbac;

public class RoleDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsSystem { get; init; }

    public bool IsDefault { get; init; }
}
