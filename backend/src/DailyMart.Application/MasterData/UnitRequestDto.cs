namespace DailyMart.Application.MasterData;

/// <summary>Used for both create and update - the writable shape is identical either way.</summary>
public class UnitRequestDto
{
    public string Name { get; init; } = string.Empty;

    public string Symbol { get; init; } = string.Empty;
}
