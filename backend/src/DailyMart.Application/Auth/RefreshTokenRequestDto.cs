namespace DailyMart.Application.Auth;

/// <summary>Shared by both /auth/refresh and /auth/logout - both operations are keyed by the same
/// presented refresh token, so a second near-identical DTO would just be needless duplication.</summary>
public class RefreshTokenRequestDto
{
    public string RefreshToken { get; init; } = string.Empty;
}
