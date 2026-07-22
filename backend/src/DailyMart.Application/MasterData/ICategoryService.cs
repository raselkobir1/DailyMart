using DailyMart.Application.Common.Models;

namespace DailyMart.Application.MasterData;

public interface ICategoryService
{
    Task<PagedResult<CategoryDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(CategoryRequestDto request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(long id, CategoryRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
