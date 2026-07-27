using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Billing;

namespace DailyMart.Application.Billing;

public class PlanService : IPlanService
{
    private readonly IUnitOfWork _unitOfWork;

    public PlanService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Plan> Repository => _unitOfWork.Repository<Plan>();

    public async Task<PagedResult<PlanDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Plan, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : plan => plan.Name.Contains(request.SearchTerm);

        var result = await Repository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<PlanDto>
        {
            Items = result.Items.Select(p => p.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<List<PlanDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var plans = await Repository.FindAsync(p => p.IsActive, cancellationToken);
        return plans.OrderBy(p => p.SortOrder).Select(p => p.ToDto()).ToList();
    }

    public async Task<PlanDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<PlanDto> CreateAsync(PlanRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var plan = request.ToEntity();
        await Repository.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }

    public async Task<PlanDto> UpdateAsync(long id, PlanRequestDto request, CancellationToken cancellationToken = default)
    {
        var plan = await GetEntityAsync(id, cancellationToken);

        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.Price = request.IsFree ? 0m : request.Price;
        plan.BillingCycle = request.BillingCycle;
        plan.IsFree = request.IsFree;
        plan.SortOrder = request.SortOrder;

        Repository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }

    public async Task<PlanDto> ActivateAsync(long id, CancellationToken cancellationToken = default) =>
        await SetActiveAsync(id, isActive: true, cancellationToken);

    public async Task<PlanDto> DeactivateAsync(long id, CancellationToken cancellationToken = default) =>
        await SetActiveAsync(id, isActive: false, cancellationToken);

    private async Task<PlanDto> SetActiveAsync(long id, bool isActive, CancellationToken cancellationToken)
    {
        var plan = await GetEntityAsync(id, cancellationToken);

        plan.IsActive = isActive;
        Repository.Update(plan);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return plan.ToDto();
    }

    private async Task<Plan> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Plan), id);

    private async Task EnsureNameIsUniqueAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var duplicateExists = await Repository.ExistsAsync(
            plan => plan.Name.ToLower() == normalizedName && (excludeId == null || plan.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A plan named '{name}' already exists.");
        }
    }
}
