namespace DailyMart.Application.Auth;

/// <summary>Admin-driven user creation - there's no self-registration (CLAUDE.md §1), so this is the only
/// way a second user ever gets into the system. Password is set here once; the new user changes it later
/// via POST /api/auth/change-password if they want to.</summary>
public class CreateUserRequestDto
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;
}
