using DailyMart.Application.Common.Models;

namespace DailyMart.Application.MasterData;

public interface IUnitService
{
    Task<PagedResult<UnitDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<UnitDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<UnitDto> CreateAsync(UnitRequestDto request, CancellationToken cancellationToken = default);

    Task<UnitDto> UpdateAsync(long id, UnitRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
