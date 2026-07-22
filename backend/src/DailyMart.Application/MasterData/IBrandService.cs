using DailyMart.Application.Common.Models;

namespace DailyMart.Application.MasterData;

public interface IBrandService
{
    Task<PagedResult<BrandDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<BrandDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<BrandDto> CreateAsync(BrandRequestDto request, CancellationToken cancellationToken = default);

    Task<BrandDto> UpdateAsync(long id, BrandRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
