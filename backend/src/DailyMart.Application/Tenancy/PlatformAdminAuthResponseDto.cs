namespace DailyMart.Application.Tenancy;

public class PlatformAdminAuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; init; }

    public string Username { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;
}
