using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.MasterData;

namespace DailyMart.Application.MasterData;

public class BrandService : IBrandService
{
    private readonly IUnitOfWork _unitOfWork;

    public BrandService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Brand> Repository => _unitOfWork.Repository<Brand>();

    public async Task<PagedResult<BrandDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Brand, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : brand => brand.Name.Contains(request.SearchTerm);

        var result = await Repository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<BrandDto>
        {
            Items = result.Items.Select(b => b.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<BrandDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<BrandDto> CreateAsync(BrandRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var brand = request.ToEntity();
        await Repository.AddAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.ToDto();
    }

    public async Task<BrandDto> UpdateAsync(long id, BrandRequestDto request, CancellationToken cancellationToken = default)
    {
        var brand = await GetEntityAsync(id, cancellationToken);

        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        brand.Name = request.Name;
        brand.Description = request.Description;

        Repository.Update(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var brand = await GetEntityAsync(id, cancellationToken);

        Repository.Remove(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Brand> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Brand), id);

    private async Task EnsureNameIsUniqueAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var duplicateExists = await Repository.ExistsAsync(
            brand => brand.Name.ToLower() == normalizedName && (excludeId == null || brand.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A brand named '{name}' already exists.");
        }
    }
}
