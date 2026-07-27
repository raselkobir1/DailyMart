using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Billing;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Billing;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<TenantSubscription> Subscriptions => _unitOfWork.Repository<TenantSubscription>();

    private IRepository<Plan> Plans => _unitOfWork.Repository<Plan>();

    private IRepository<SubscriptionPayment> Payments => _unitOfWork.Repository<SubscriptionPayment>();

    public async Task<TenantSubscriptionDto> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetSubscriptionEntityAsync(tenantId, cancellationToken);
        var plan = await GetPlanEntityAsync(subscription.PlanId, cancellationToken);
        var tenant = await _unitOfWork.Repository<Tenant>().GetByIdAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        return ToDto(subscription, plan, tenant.Name);
    }

    public async Task<PagedResult<SubscriptionPaymentDto>> GetPaymentHistoryAsync(
        long tenantId, PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await Payments.GetPagedAsync(request, sp => sp.TenantId == tenantId, cancellationToken);

        return new PagedResult<SubscriptionPaymentDto>
        {
            Items = result.Items.Select(p => p.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<TenantSubscriptionDto> ChangePlanAsync(
        long tenantId, long planId, CancellationToken cancellationToken = default)
    {
        var subscription = await GetSubscriptionEntityAsync(tenantId, cancellationToken);
        var newPlan = await GetPlanEntityAsync(planId, cancellationToken);

        if (!newPlan.IsActive)
        {
            throw new BusinessRuleException($"Plan '{newPlan.Name}' is retired and can't be newly assigned.");
        }

        subscription.PlanId = newPlan.Id;
        subscription.CurrentPeriodStart = DateTimeOffset.UtcNow;

        // Free never expires. Free -> paid deliberately leaves CurrentPeriodEnd null (reads as Overdue
        // immediately, prompting a payment). Paid -> paid keeps the existing CurrentPeriodEnd untouched.
        if (newPlan.IsFree)
        {
            subscription.CurrentPeriodEnd = null;
        }

        Subscriptions.Update(subscription);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tenant = await _unitOfWork.Repository<Tenant>().GetByIdAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), tenantId);

        return ToDto(subscription, newPlan, tenant.Name);
    }

    public async Task<SubscriptionPaymentDto> RecordPaymentAsync(
        long tenantId, RecordPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        var subscription = await GetSubscriptionEntityAsync(tenantId, cancellationToken);
        var plan = await GetPlanEntityAsync(subscription.PlanId, cancellationToken);

        if (plan.IsFree)
        {
            throw new BusinessRuleException(
                "This tenant is on the Free plan - switch them to a paid plan before recording a payment.");
        }

        var now = DateTimeOffset.UtcNow;
        var periodStart = subscription.CurrentPeriodEnd is { } currentEnd && currentEnd > now ? currentEnd : now;

        var payment = new SubscriptionPayment
        {
            TenantId = tenantId,
            PlanId = plan.Id,
            Amount = request.Amount,
            PeriodStart = periodStart,
            PeriodEnd = request.PaidUntil,
            Method = request.Method,
            Notes = request.Notes
        };
        await Payments.AddAsync(payment, cancellationToken);

        subscription.CurrentPeriodStart = periodStart;
        subscription.CurrentPeriodEnd = request.PaidUntil;
        Subscriptions.Update(subscription);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return payment.ToDto();
    }

    public async Task<Dictionary<long, TenantSubscriptionDto>> GetSummariesByTenantIdsAsync(
        IEnumerable<long> tenantIds, CancellationToken cancellationToken = default)
    {
        var ids = tenantIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<long, TenantSubscriptionDto>();
        }

        var subscriptions = await Subscriptions.FindAsync(ts => ids.Contains(ts.TenantId), cancellationToken);
        if (subscriptions.Count == 0)
        {
            return new Dictionary<long, TenantSubscriptionDto>();
        }

        var planIds = subscriptions.Select(s => s.PlanId).Distinct().ToList();
        var plans = (await Plans.FindAsync(p => planIds.Contains(p.Id), cancellationToken)).ToDictionary(p => p.Id);

        var tenants = (await _unitOfWork.Repository<Tenant>().FindAsync(t => ids.Contains(t.Id), cancellationToken))
            .ToDictionary(t => t.Id);

        var result = new Dictionary<long, TenantSubscriptionDto>();
        foreach (var subscription in subscriptions)
        {
            if (!plans.TryGetValue(subscription.PlanId, out var plan) || !tenants.TryGetValue(subscription.TenantId, out var tenant))
            {
                continue;
            }

            result[subscription.TenantId] = ToDto(subscription, plan, tenant.Name);
        }

        return result;
    }

    private static TenantSubscriptionDto ToDto(TenantSubscription subscription, Plan plan, string tenantName)
    {
        var isOverdue = !plan.IsFree
            && (subscription.CurrentPeriodEnd is null || subscription.CurrentPeriodEnd < DateTimeOffset.UtcNow);

        return new TenantSubscriptionDto
        {
            TenantId = subscription.TenantId,
            TenantName = tenantName,
            PlanId = plan.Id,
            PlanName = plan.Name,
            IsFree = plan.IsFree,
            Price = plan.Price,
            CurrentPeriodStart = subscription.CurrentPeriodStart,
            CurrentPeriodEnd = subscription.CurrentPeriodEnd,
            IsOverdue = isOverdue
        };
    }

    private async Task<TenantSubscription> GetSubscriptionEntityAsync(long tenantId, CancellationToken cancellationToken)
    {
        var subscriptions = await Subscriptions.FindAsync(ts => ts.TenantId == tenantId, cancellationToken);
        return subscriptions.FirstOrDefault() ?? throw new NotFoundException(nameof(TenantSubscription), tenantId);
    }

    private async Task<Plan> GetPlanEntityAsync(long planId, CancellationToken cancellationToken) =>
        await Plans.GetByIdAsync(planId, cancellationToken) ?? throw new NotFoundException(nameof(Plan), planId);
}
