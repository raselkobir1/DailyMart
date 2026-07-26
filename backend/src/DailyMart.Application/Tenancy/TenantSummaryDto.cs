namespace DailyMart.Application.Tenancy;

public class TenantSummaryDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
