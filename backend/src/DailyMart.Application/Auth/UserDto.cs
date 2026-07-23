namespace DailyMart.Application.Auth;

public class UserDto
{
    public long Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
