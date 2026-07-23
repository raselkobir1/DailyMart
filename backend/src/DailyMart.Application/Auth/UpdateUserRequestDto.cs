namespace DailyMart.Application.Auth;

/// <summary>No Username/Password here - identity and credentials aren't admin-editable; Username is
/// immutable and password changes go through the user's own POST /api/auth/change-password.</summary>
public class UpdateUserRequestDto
{
    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
