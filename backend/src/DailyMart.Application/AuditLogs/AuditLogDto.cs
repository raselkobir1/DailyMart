namespace DailyMart.Application.AuditLogs;

public class AuditLogDto
{
    public long Id { get; init; }

    public string EntityName { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string? OldValues { get; init; }

    public string? NewValues { get; init; }

    public string? ChangedColumns { get; init; }

    public string PerformedBy { get; init; } = string.Empty;

    public DateTimeOffset PerformedAt { get; init; }
}
