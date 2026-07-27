namespace DailyMart.Application.Billing;

public class PlanDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public decimal Price { get; init; }

    public string BillingCycle { get; init; } = string.Empty;

    public bool IsFree { get; init; }

    public bool IsActive { get; init; }

    public int SortOrder { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
