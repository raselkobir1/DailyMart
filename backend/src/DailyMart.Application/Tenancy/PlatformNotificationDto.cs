namespace DailyMart.Application.Tenancy;

public class PlatformNotificationDto
{
    public long Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public long? TenantId { get; init; }

    public string TenantName { get; init; } = string.Empty;

    public string? AdminUsername { get; init; }

    public bool IsRead { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
