using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Billing;

public interface IPlanService
{
    Task<PagedResult<PlanDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    /// <summary>Active plans only, ordered by SortOrder - feeds the "change plan" dropdown so a retired
    /// plan can't be newly assigned to a tenant.</summary>
    Task<List<PlanDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<PlanDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<PlanDto> CreateAsync(PlanRequestDto request, CancellationToken cancellationToken = default);

    Task<PlanDto> UpdateAsync(long id, PlanRequestDto request, CancellationToken cancellationToken = default);

    Task<PlanDto> ActivateAsync(long id, CancellationToken cancellationToken = default);

    Task<PlanDto> DeactivateAsync(long id, CancellationToken cancellationToken = default);
}
