namespace DailyMart.Application.Auth;

public class ChangePasswordRequestDto
{
    public string CurrentPassword { get; init; } = string.Empty;

    public string NewPassword { get; init; } = string.Empty;
}
