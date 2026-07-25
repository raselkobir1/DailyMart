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

    /// <summary>Every product, unpaginated - backs the CSV export endpoint.</summary>
    Task<IReadOnlyList<ProductDto>> GetAllForExportAsync(CancellationToken cancellationToken = default);

    /// <summary>Bulk create/update from an uploaded .xlsx workbook (first worksheet, header row + data
    /// rows) - Category/Brand/Unit are matched by name (not id), since a human filling the template has no
    /// way to know internal ids. A row whose Code matches an existing product is treated as an update
    /// (reusing UpdateAsync's own validation/business rules); any other row is a create (reusing
    /// CreateAsync). Each row commits independently - one bad row is reported as an error but doesn't roll
    /// back the rows already imported before or after it, since forcing an all-or-nothing outcome would
    /// make a single typo in a 500-row sheet discard 499 good imports.</summary>
    Task<ProductImportResultDto> ImportAsync(Stream fileContent, CancellationToken cancellationToken = default);

    /// <summary>A blank .xlsx with the exact headers ImportAsync expects, one example row, and a reference
    /// sheet listing every current Category/Brand/Unit name - since those three columns are matched by
    /// name, this is what tells the person filling it out which spellings will actually resolve.</summary>
    Task<byte[]> BuildImportTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>Products where CurrentStock has fallen to or below MinimumStock - lives here rather than
    /// on IInventoryService (Module 8) since it only ever queries Product and reuses this service's own
    /// DTO/lookup mapping, touching no InventoryTransaction/InventoryAdjustment data at all.</summary>
    Task<PagedResult<ProductDto>> GetLowStockAsync(PagedRequest request, CancellationToken cancellationToken = default);
}
