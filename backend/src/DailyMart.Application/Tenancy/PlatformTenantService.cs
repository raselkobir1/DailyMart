using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.Tenancy;

namespace DailyMart.Application.Tenancy;

public class PlatformTenantService : IPlatformTenantService
{
    private readonly IUnitOfWork _unitOfWork;

    public PlatformTenantService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<TenantSummaryDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _unitOfWork.Repository<Tenant>().GetPagedAsync(request, predicate: null, cancellationToken);

        return new PagedResult<TenantSummaryDto>
        {
            Items = result.Items.Select(ToDto).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<TenantSummaryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        ToDto(await GetEntityAsync(id, cancellationToken));

    public async Task<TenantSummaryDto> SetActiveAsync(
        long id, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenant = await GetEntityAsync(id, cancellationToken);

        tenant.IsActive = isActive;
        _unitOfWork.Repository<Tenant>().Update(tenant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(tenant);
    }

    private async Task<Tenant> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Tenant>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), id);

    private static TenantSummaryDto ToDto(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        IsActive = tenant.IsActive,
        CreatedAt = tenant.CreatedAt
    };
}
