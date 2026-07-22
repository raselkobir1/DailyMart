namespace DailyMart.Application.MasterData;

public class CategoryDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
