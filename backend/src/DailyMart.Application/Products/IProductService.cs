using DailyMart.Application.Common.Models;

namespace DailyMart.Application.Products;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(PagedRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>What the POS barcode-scanner workflow (Module 9) will call - throws NotFoundException if
    /// nothing matches, same "missing" contract as GetByIdAsync.</summary>
    Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Never touches CurrentStock - see Module 4 Step 1's scope decision.</summary>
    Task<ProductDto> UpdateAsync(long id, ProductRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task<ProductDto> UploadImageAsync(
        long id, Stream fileContent, string fileName, CancellationToken cancellationToken = default);

    /// <summary>Every product, unpaginated - backs the CSV export endpoint. Import is a deferred
    /// fast-follow (Module 4 Step 1's scope decision), so there's no counterpart bulk-create method yet.</summary>
    Task<IReadOnlyList<ProductDto>> GetAllForExportAsync(CancellationToken cancellationToken = default);

    /// <summary>Products where CurrentStock has fallen to or below MinimumStock - lives here rather than
    /// on IInventoryService (Module 8) since it only ever queries Product and reuses this service's own
    /// DTO/lookup mapping, touching no InventoryTransaction/InventoryAdjustment data at all.</summary>
    Task<PagedResult<ProductDto>> GetLowStockAsync(PagedRequest request, CancellationToken cancellationToken = default);
}
