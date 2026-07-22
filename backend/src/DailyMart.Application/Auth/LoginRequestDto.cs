namespace DailyMart.Application.Auth;

public class LoginRequestDto
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
