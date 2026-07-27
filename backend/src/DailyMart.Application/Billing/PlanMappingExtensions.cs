using DailyMart.Domain.Billing;

namespace DailyMart.Application.Billing;

internal static class PlanMappingExtensions
{
    public static PlanDto ToDto(this Plan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        BillingCycle = plan.BillingCycle.ToString(),
        IsFree = plan.IsFree,
        IsActive = plan.IsActive,
        SortOrder = plan.SortOrder,
        CreatedAt = plan.CreatedAt
    };

    public static Plan ToEntity(this PlanRequestDto request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        Price = request.IsFree ? 0m : request.Price,
        BillingCycle = request.BillingCycle,
        IsFree = request.IsFree,
        SortOrder = request.SortOrder
    };
}
