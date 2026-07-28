namespace DailyMart.Application.Tenancy;

public class SupportMessageDto
{
    public long Id { get; init; }

    public long TenantId { get; init; }

    public bool FromPlatformAdmin { get; init; }

    /// <summary>The sender's username - SupportMessage.CreatedBy, see that entity's doc comment.</summary>
    public string SenderName { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}
