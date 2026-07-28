namespace DailyMart.Application.Billing;

public class TenantReminderEmailResultDto
{
    public string SentTo { get; init; } = string.Empty;

    /// <summary>"Overdue" or "Free" - which content was actually sent, so the platform-admin UI can
    /// confirm what the tenant was told rather than just "an email was sent."</summary>
    public string ReminderType { get; init; } = string.Empty;
}
