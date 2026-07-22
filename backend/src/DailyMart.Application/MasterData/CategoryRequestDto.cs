namespace DailyMart.Application.MasterData;

/// <summary>Used for both create and update - the writable shape is identical either way.</summary>
public class CategoryRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
}
