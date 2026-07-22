using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.MasterData;

namespace DailyMart.Application.MasterData;

public class UnitService : IUnitService
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private IRepository<Unit> Repository => _unitOfWork.Repository<Unit>();

    public async Task<PagedResult<UnitDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Unit, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : unit => unit.Name.Contains(request.SearchTerm);

        var result = await Repository.GetPagedAsync(request, predicate, cancellationToken);

        return new PagedResult<UnitDto>
        {
            Items = result.Items.Select(u => u.ToDto()).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<UnitDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        (await GetEntityAsync(id, cancellationToken)).ToDto();

    public async Task<UnitDto> CreateAsync(UnitRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var unit = request.ToEntity();
        await Repository.AddAsync(unit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return unit.ToDto();
    }

    public async Task<UnitDto> UpdateAsync(long id, UnitRequestDto request, CancellationToken cancellationToken = default)
    {
        var unit = await GetEntityAsync(id, cancellationToken);

        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        unit.Name = request.Name;
        unit.Symbol = request.Symbol;

        Repository.Update(unit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return unit.ToDto();
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var unit = await GetEntityAsync(id, cancellationToken);

        Repository.Remove(unit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Unit> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await Repository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Unit), id);

    private async Task EnsureNameIsUniqueAsync(string name, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var duplicateExists = await Repository.ExistsAsync(
            unit => unit.Name.ToLower() == normalizedName && (excludeId == null || unit.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A unit named '{name}' already exists.");
        }
    }
}
