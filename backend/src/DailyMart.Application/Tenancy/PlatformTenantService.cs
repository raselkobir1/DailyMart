using DailyMart.Application.Billing;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Tenancy;

public class PlatformTenantService : IPlatformTenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionService _subscriptionService;

    public PlatformTenantService(IUnitOfWork unitOfWork, ISubscriptionService subscriptionService)
    {
        _unitOfWork = unitOfWork;
        _subscriptionService = subscriptionService;
    }

    public async Task<PagedResult<TenantSummaryDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.Repository<Tenant>().GetPagedAsync(request, predicate: null, cancellationToken);

        var subscriptions = await _subscriptionService.GetSummariesByTenantIdsAsync(
            result.Items.Select(t => t.Id), cancellationToken);

        return new PagedResult<TenantSummaryDto>
        {
            Items = result.Items.Select(t => ToDto(t, subscriptions.GetValueOrDefault(t.Id))).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<TenantSummaryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEntityAsync(id, cancellationToken);
        var subscriptions = await _subscriptionService.GetSummariesByTenantIdsAsync([id], cancellationToken);
        return ToDto(tenant, subscriptions.GetValueOrDefault(id));
    }

    public async Task<TenantSummaryDto> SetActiveAsync(
        long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEntityAsync(id, cancellationToken);

        tenant.IsActive = isActive;
        _unitOfWork.Repository<Tenant>().Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var subscriptions = await _subscriptionService.GetSummariesByTenantIdsAsync([id], cancellationToken);
        return ToDto(tenant, subscriptions.GetValueOrDefault(id));
    }

    private async Task<Tenant> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Tenant>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), id);

    private static TenantSummaryDto ToDto(Tenant tenant, TenantSubscriptionDto? subscription) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        IsActive = tenant.IsActive,
        CreatedAt = tenant.CreatedAt,
        PlanName = subscription?.PlanName,
        IsFree = subscription?.IsFree ?? false,
        CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
        IsOverdue = subscription?.IsOverdue ?? false
    };
}
