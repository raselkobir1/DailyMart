using DailyMart.Application.Billing;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.UsageAnalytics;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Tenancy;

/// <summary>
/// GetPagedAsync enriches every tenant with billing/usage data before filtering, sorting, or paging -
/// unlike every other module's paged list (page at the DB level, enrich just that page), the fields
/// worth sorting/filtering by here (Overdue, Last Active, Users, ...) only exist after the billing/usage
/// join, so paging first would page over the wrong ordering entirely. Fetching every tenant is fine at
/// this panel's actual scale (a platform admin's whole company list, not per-tenant business data).
/// </summary>
public class PlatformTenantService : IPlatformTenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUsageAnalyticsService _usageAnalyticsService;

    public PlatformTenantService(
        IUnitOfWork unitOfWork, ISubscriptionService subscriptionService, IUsageAnalyticsService usageAnalyticsService)
    {
        _unitOfWork = unitOfWork;
        _subscriptionService = subscriptionService;
        _usageAnalyticsService = usageAnalyticsService;
    }

    public async Task<PagedResult<TenantSummaryDto>> GetPagedAsync(
        PagedRequest request, string? status = null, string? billingStatus = null, CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Repository<Tenant>().GetAllAsync(cancellationToken);
        var tenantIds = tenants.Select(t => t.Id).ToList();

        var subscriptions = await _subscriptionService.GetSummariesByTenantIdsAsync(tenantIds, cancellationToken);
        var usageSnapshots = await _usageAnalyticsService.GetSnapshotsByTenantIdsAsync(tenantIds, cancellationToken);

        IEnumerable<TenantSummaryDto> dtos = tenants
            .Select(t => ToDto(t, subscriptions.GetValueOrDefault(t.Id), usageSnapshots.GetValueOrDefault(t.Id)));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            dtos = dtos.Where(d => d.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        dtos = status?.ToLowerInvariant() switch
        {
            "active" => dtos.Where(d => d.IsActive),
            "suspended" => dtos.Where(d => !d.IsActive),
            _ => dtos
        };

        dtos = billingStatus?.ToLowerInvariant() switch
        {
            "overdue" => dtos.Where(d => d.IsOverdue),
            "paid" => dtos.Where(d => !d.IsFree && !d.IsOverdue),
            "free" => dtos.Where(d => d.IsFree),
            _ => dtos
        };

        var sorted = ApplySort(dtos, request.SortBy, request.SortDescending).ToList();
        var page = sorted.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new PagedResult<TenantSummaryDto>
        {
            Items = page,
            TotalCount = sorted.Count,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    private static IEnumerable<TenantSummaryDto> ApplySort(
        IEnumerable<TenantSummaryDto> items, string? sortBy, bool descending)
    {
        Func<TenantSummaryDto, object?> keySelector = sortBy?.ToLowerInvariant() switch
        {
            "name" => d => d.Name,
            "status" => d => d.IsActive,
            "plan" or "planname" => d => d.PlanName,
            "paiduntil" or "currentperiodend" => d => d.CurrentPeriodEnd,
            "users" or "totalusers" => d => d.TotalUsers,
            "lastactive" or "lastactiveat" => d => d.LastActiveAt,
            "created" or "createdat" => d => d.CreatedAt,
            _ => d => d.Id
        };

        return descending ? items.OrderByDescending(keySelector) : items.OrderBy(keySelector);
    }

    public async Task<TenantSummaryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEntityAsync(id, cancellationToken);
        var subscriptions = await _subscriptionService.GetSummariesByTenantIdsAsync([id], cancellationToken);
        var usageSnapshots = await _usageAnalyticsService.GetSnapshotsByTenantIdsAsync([id], cancellationToken);
        return ToDto(tenant, subscriptions.GetValueOrDefault(id), usageSnapshots.GetValueOrDefault(id));
    }

    public async Task<TenantSummaryDto> SetActiveAsync(
        long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEntityAsync(id, cancellationToken);

        tenant.IsActive = isActive;
        _unitOfWork.Repository<Tenant>().Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var subscriptions = await _subscriptionService.GetSummariesByTenantIdsAsync([id], cancellationToken);
        var usageSnapshots = await _usageAnalyticsService.GetSnapshotsByTenantIdsAsync([id], cancellationToken);
        return ToDto(tenant, subscriptions.GetValueOrDefault(id), usageSnapshots.GetValueOrDefault(id));
    }

    private async Task<Tenant> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Tenant>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), id);

    private static TenantSummaryDto ToDto(
        Tenant tenant, TenantSubscriptionDto? subscription, TenantUsageSnapshotDto? usage) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        IsActive = tenant.IsActive,
        CreatedAt = tenant.CreatedAt,
        PlanName = subscription?.PlanName,
        IsFree = subscription?.IsFree ?? false,
        CurrentPeriodEnd = subscription?.CurrentPeriodEnd,
        IsOverdue = subscription?.IsOverdue ?? false,
        TotalUsers = usage?.TotalUsers ?? 0,
        ActiveUsers = usage?.ActiveUsers ?? 0,
        LastLoginAt = usage?.LastLoginAt,
        LastActivityAt = usage?.LastActivityAt,
        LastActiveAt = ComputeLastActiveAt(usage)
    };

    private static DateTimeOffset? ComputeLastActiveAt(TenantUsageSnapshotDto? usage)
    {
        if (usage is null)
        {
            return null;
        }

        if (usage.LastLoginAt is null)
        {
            return usage.LastActivityAt;
        }

        if (usage.LastActivityAt is null)
        {
            return usage.LastLoginAt;
        }

        return usage.LastLoginAt > usage.LastActivityAt ? usage.LastLoginAt : usage.LastActivityAt;
    }
}
