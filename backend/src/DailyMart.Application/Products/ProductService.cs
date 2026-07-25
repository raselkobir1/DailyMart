using System.Linq.Expressions;
using ClosedXML.Excel;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.MasterData;
using DailyMart.Domain.Products;
using FluentValidation;

namespace DailyMart.Application.Products;

public class ProductService : IProductService
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageSizeBytes = 2 * 1024 * 1024;
    private const int MaxBarcodeGenerationAttempts = 5;

    private static readonly string[] ImportHeaders =
    [
        "Code", "Barcode", "Name", "Category", "Brand", "Unit", "PurchasePrice", "SellingPrice",
        "WholesalePrice", "DiscountPercentage", "TaxPercentage", "CurrentStock", "MinimumStock",
        "AllowPriceBelowCost"
    ];

    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly IValidator<CreateProductRequestDto> _createValidator;
    private readonly IValidator<ProductRequestDto> _updateValidator;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorageService,
        IValidator<CreateProductRequestDto> createValidator,
        IValidator<ProductRequestDto> updateValidator)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        Expression<Func<Product, bool>>? predicate = string.IsNullOrWhiteSpace(request.SearchTerm)
            ? null
            : product => product.Name.Contains(request.SearchTerm)
                || product.Code.Contains(request.SearchTerm)
                || product.Barcode.Contains(request.SearchTerm);

        var result = await _productRepository.GetPagedAsync(request, predicate, cancellationToken);
        var lookups = await BuildLookupsAsync(result.Items, cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(p => p.ToDto(lookups)).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<ProductDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        await MapToDtoAsync(await GetEntityAsync(id, cancellationToken), cancellationToken);

    public async Task<ProductDto> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByBarcodeAsync(barcode, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), barcode);

        return await MapToDtoAsync(product, cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(
        CreateProductRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateReferencesExistAsync(request.CategoryId, request.BrandId, request.UnitId, cancellationToken);
        await EnsureCodeIsUniqueAsync(request.Code, excludeId: null, cancellationToken);

        var product = request.ToEntity();
        product.Code = NormalizeCode(product.Code);
        ValidatePricing(product);

        product.Barcode = string.IsNullOrWhiteSpace(request.Barcode)
            ? await GenerateUniqueBarcodeAsync(cancellationToken)
            : await NormalizeAndValidateBarcodeAsync(request.Barcode, excludeId: null, cancellationToken);

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(product, cancellationToken);
    }

    public async Task<ProductDto> UpdateAsync(
        long id, ProductRequestDto request, CancellationToken cancellationToken = default)
    {
        var product = await GetEntityAsync(id, cancellationToken);

        await ValidateReferencesExistAsync(request.CategoryId, request.BrandId, request.UnitId, cancellationToken);
        await EnsureCodeIsUniqueAsync(request.Code, id, cancellationToken);

        var barcode = string.IsNullOrWhiteSpace(request.Barcode) ? product.Barcode : request.Barcode;
        if (!string.Equals(barcode, product.Barcode, StringComparison.Ordinal))
        {
            barcode = await NormalizeAndValidateBarcodeAsync(barcode, id, cancellationToken);
        }

        request.ApplyTo(product);
        product.Code = NormalizeCode(product.Code);
        product.Barcode = barcode;

        ValidatePricing(product);

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(product, cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var product = await GetEntityAsync(id, cancellationToken);

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductDto> UploadImageAsync(
        long id, Stream fileContent, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedImageExtensions.Contains(extension))
        {
            throw new BusinessRuleException(
                $"Unsupported image file type '{extension}'. Allowed types: {string.Join(", ", AllowedImageExtensions)}.");
        }

        if (fileContent.Length > MaxImageSizeBytes)
        {
            throw new BusinessRuleException(
                $"Image file exceeds the maximum size of {MaxImageSizeBytes / (1024 * 1024)} MB.");
        }

        var product = await GetEntityAsync(id, cancellationToken);

        var url = await _fileStorageService.SaveAsync(fileContent, fileName, "products", cancellationToken);

        product.ImageUrl = url;
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(product, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllForExportAsync(CancellationToken cancellationToken = default)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);
        var lookups = await BuildLookupsAsync(products, cancellationToken);

        return products.Select(p => p.ToDto(lookups)).ToList();
    }

    public async Task<PagedResult<ProductDto>> GetLowStockAsync(
        PagedRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _productRepository.GetPagedAsync(
            request, product => product.CurrentStock <= product.MinimumStock, cancellationToken);
        var lookups = await BuildLookupsAsync(result.Items, cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = result.Items.Select(p => p.ToDto(lookups)).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize
        };
    }

    public async Task<ProductImportResultDto> ImportAsync(
        Stream fileContent, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook(fileContent);
        var worksheet = workbook.Worksheets.First();

        var columns = ReadHeaderColumns(worksheet);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        var categoryIdsByName = (await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Name.Trim(), c => c.Id, StringComparer.OrdinalIgnoreCase);
        var brandIdsByName = (await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken))
            .ToDictionary(b => b.Name.Trim(), b => b.Id, StringComparer.OrdinalIgnoreCase);
        var unitIdsByName = (await _unitOfWork.Repository<Unit>().GetAllAsync(cancellationToken))
            .ToDictionary(u => u.Name.Trim(), u => u.Id, StringComparer.OrdinalIgnoreCase);
        var existingIdsByCode = (await _productRepository.GetAllAsync(cancellationToken))
            .ToDictionary(p => NormalizeCode(p.Code), p => p.Id);

        var totalRows = 0;
        var created = 0;
        var updated = 0;
        var errors = new List<ProductImportRowErrorDto>();

        for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
        {
            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
            {
                continue;
            }

            totalRows++;

            try
            {
                var code = row.Cell(columns["Code"]).GetString().Trim();
                var categoryName = row.Cell(columns["Category"]).GetString().Trim();
                var brandName = row.Cell(columns["Brand"]).GetString().Trim();
                var unitName = row.Cell(columns["Unit"]).GetString().Trim();

                if (!categoryIdsByName.TryGetValue(categoryName, out var categoryId))
                {
                    throw new BusinessRuleException($"Category '{categoryName}' does not exist.");
                }

                if (!unitIdsByName.TryGetValue(unitName, out var unitId))
                {
                    throw new BusinessRuleException($"Unit '{unitName}' does not exist.");
                }

                long? brandId = null;
                if (brandName.Length > 0)
                {
                    if (!brandIdsByName.TryGetValue(brandName, out var resolvedBrandId))
                    {
                        throw new BusinessRuleException($"Brand '{brandName}' does not exist.");
                    }

                    brandId = resolvedBrandId;
                }

                var barcode = row.Cell(columns["Barcode"]).GetString().Trim();
                var wholesalePrice = ParseNullableDecimal(row.Cell(columns["WholesalePrice"]));

                var requestDto = new ProductRequestDto
                {
                    Code = code,
                    Barcode = barcode.Length > 0 ? barcode : null,
                    Name = row.Cell(columns["Name"]).GetString().Trim(),
                    CategoryId = categoryId,
                    BrandId = brandId,
                    UnitId = unitId,
                    PurchasePrice = ParseNullableDecimal(row.Cell(columns["PurchasePrice"]))
                        ?? throw new ProductImportRowException("'PurchasePrice' is required."),
                    SellingPrice = ParseNullableDecimal(row.Cell(columns["SellingPrice"]))
                        ?? throw new ProductImportRowException("'SellingPrice' is required."),
                    WholesalePrice = wholesalePrice,
                    DiscountPercentage = ParseNullableDecimal(row.Cell(columns["DiscountPercentage"])) ?? 0,
                    TaxPercentage = ParseNullableDecimal(row.Cell(columns["TaxPercentage"])) ?? 0,
                    MinimumStock = ParseNullableDecimal(row.Cell(columns["MinimumStock"])) ?? 0,
                    AllowPriceBelowCost = ParseBool(row.Cell(columns["AllowPriceBelowCost"]))
                };

                if (existingIdsByCode.TryGetValue(NormalizeCode(code), out var existingId))
                {
                    await ValidateAsync(_updateValidator, requestDto, rowNumber, cancellationToken);
                    await UpdateAsync(existingId, requestDto, cancellationToken);
                    updated++;
                }
                else
                {
                    var createDto = new CreateProductRequestDto
                    {
                        Code = requestDto.Code,
                        Barcode = requestDto.Barcode,
                        Name = requestDto.Name,
                        CategoryId = requestDto.CategoryId,
                        BrandId = requestDto.BrandId,
                        UnitId = requestDto.UnitId,
                        PurchasePrice = requestDto.PurchasePrice,
                        SellingPrice = requestDto.SellingPrice,
                        WholesalePrice = requestDto.WholesalePrice,
                        DiscountPercentage = requestDto.DiscountPercentage,
                        TaxPercentage = requestDto.TaxPercentage,
                        MinimumStock = requestDto.MinimumStock,
                        AllowPriceBelowCost = requestDto.AllowPriceBelowCost,
                        CurrentStock = ParseNullableDecimal(row.Cell(columns["CurrentStock"])) ?? 0
                    };

                    await ValidateAsync(_createValidator, createDto, rowNumber, cancellationToken);
                    var newProduct = await CreateAsync(createDto, cancellationToken);
                    existingIdsByCode[NormalizeCode(newProduct.Code)] = newProduct.Id;
                    created++;
                }
            }
            catch (Exception ex)
            {
                // Any failure for this row (bad reference name, failed validation, unparsable cell, a
                // duplicate code/barcode caught by Create/UpdateAsync's own checks) degrades to a per-row
                // error rather than aborting the rest of the file - a single bad row in a large sheet
                // shouldn't cost the user every other row they got right.
                errors.Add(new ProductImportRowErrorDto { RowNumber = rowNumber, Message = ex.Message });
            }
        }

        return new ProductImportResultDto { TotalRows = totalRows, Created = created, Updated = updated, Errors = errors };
    }

    public Task<byte[]> BuildImportTemplateAsync(CancellationToken cancellationToken = default) =>
        BuildImportTemplateInternalAsync(cancellationToken);

    private async Task<byte[]> BuildImportTemplateInternalAsync(CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync(cancellationToken);
        var brands = await _unitOfWork.Repository<Brand>().GetAllAsync(cancellationToken);
        var units = await _unitOfWork.Repository<Unit>().GetAllAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");

        for (var i = 0; i < ImportHeaders.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = ImportHeaders[i];
        }

        var exampleCategory = categories.FirstOrDefault()?.Name ?? "Grocery";
        var exampleUnit = units.FirstOrDefault()?.Name ?? "Piece";
        sheet.Cell(2, 1).Value = "SAMPLE-001";
        sheet.Cell(2, 3).Value = "Sample Product";
        sheet.Cell(2, 4).Value = exampleCategory;
        sheet.Cell(2, 6).Value = exampleUnit;
        sheet.Cell(2, 7).Value = 50;
        sheet.Cell(2, 8).Value = 75;
        sheet.Cell(2, 12).Value = 100;
        sheet.Cell(2, 13).Value = 5;
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        var reference = workbook.Worksheets.Add("Valid Category-Brand-Unit Names");
        reference.Cell(1, 1).Value = "Category";
        reference.Cell(1, 2).Value = "Brand";
        reference.Cell(1, 3).Value = "Unit";
        reference.Row(1).Style.Font.Bold = true;

        var categoryNames = categories.Select(c => c.Name).OrderBy(n => n).ToList();
        var brandNames = brands.Select(b => b.Name).OrderBy(n => n).ToList();
        var unitNames = units.Select(u => u.Name).OrderBy(n => n).ToList();
        var maxRows = new[] { categoryNames.Count, brandNames.Count, unitNames.Count }.Max();

        for (var i = 0; i < maxRows; i++)
        {
            if (i < categoryNames.Count) reference.Cell(i + 2, 1).Value = categoryNames[i];
            if (i < brandNames.Count) reference.Cell(i + 2, 2).Value = brandNames[i];
            if (i < unitNames.Count) reference.Cell(i + 2, 3).Value = unitNames[i];
        }

        reference.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static Dictionary<string, int> ReadHeaderColumns(IXLWorksheet worksheet)
    {
        var headerRow = worksheet.Row(1);
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (var col = 1; col <= lastColumn; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            if (header.Length > 0)
            {
                columns[header] = col;
            }
        }

        var missing = ImportHeaders
            .Where(h => h is not ("Barcode" or "Brand" or "WholesalePrice" or "DiscountPercentage"
                or "TaxPercentage" or "CurrentStock" or "AllowPriceBelowCost"))
            .Where(required => !columns.ContainsKey(required))
            .ToList();

        if (missing.Count > 0)
        {
            throw new BusinessRuleException(
                $"The uploaded file is missing required column(s): {string.Join(", ", missing)}.");
        }

        return columns;
    }

    private static decimal? ParseNullableDecimal(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (!cell.TryGetValue(out decimal value))
        {
            throw new ProductImportRowException($"'{cell.GetString()}' is not a valid number.");
        }

        return value;
    }

    private static bool ParseBool(IXLCell cell)
    {
        var raw = cell.GetString().Trim();
        return raw.Length > 0
            && (raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || raw == "1");
    }

    private static async Task ValidateAsync<T>(
        IValidator<T> validator, T instance, int rowNumber, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new ProductImportRowException(string.Join(' ', result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    private async Task<Product> GetEntityAsync(long id, CancellationToken cancellationToken) =>
        await _productRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(Product), id);

    private async Task<ProductDto> MapToDtoAsync(Product product, CancellationToken cancellationToken)
    {
        var lookups = await BuildLookupsAsync([product], cancellationToken);
        return product.ToDto(lookups);
    }

    private async Task<ProductLookups> BuildLookupsAsync(
        IReadOnlyCollection<Product> products, CancellationToken cancellationToken)
    {
        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var brandIds = products.Where(p => p.BrandId is not null).Select(p => p.BrandId!.Value).Distinct().ToList();
        var unitIds = products.Select(p => p.UnitId).Distinct().ToList();

        var categories = await _unitOfWork.Repository<Category>()
            .FindAsync(c => categoryIds.Contains(c.Id), cancellationToken);
        var brands = await _unitOfWork.Repository<Brand>()
            .FindAsync(b => brandIds.Contains(b.Id), cancellationToken);
        var units = await _unitOfWork.Repository<Unit>()
            .FindAsync(u => unitIds.Contains(u.Id), cancellationToken);

        return new ProductLookups(
            categories.ToDictionary(c => c.Id, c => c.Name),
            brands.ToDictionary(b => b.Id, b => b.Name),
            units.ToDictionary(u => u.Id, u => (u.Name, u.Symbol)));
    }

    private static void ValidatePricing(Product product)
    {
        if (!product.AllowPriceBelowCost && product.SellingPrice < product.PurchasePrice)
        {
            throw new BusinessRuleException(
                "Selling price cannot be lower than purchase price unless 'Allow Price Below Cost' is enabled.");
        }
    }

    private async Task ValidateReferencesExistAsync(
        long categoryId, long? brandId, long unitId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Category>().ExistsAsync(c => c.Id == categoryId, cancellationToken))
        {
            throw new BusinessRuleException($"Category with id '{categoryId}' does not exist.");
        }

        if (!await _unitOfWork.Repository<Unit>().ExistsAsync(u => u.Id == unitId, cancellationToken))
        {
            throw new BusinessRuleException($"Unit with id '{unitId}' does not exist.");
        }

        if (brandId is not null
            && !await _unitOfWork.Repository<Brand>().ExistsAsync(b => b.Id == brandId, cancellationToken))
        {
            throw new BusinessRuleException($"Brand with id '{brandId}' does not exist.");
        }
    }

    /// <summary>Canonicalizes Code to a single case before it's ever persisted or compared - the DB's
    /// unique index on Code is case-SENSITIVE, but EnsureCodeIsUniqueAsync's check below is case-
    /// INSENSITIVE. Without this, two concurrent creates using different casing of the same code (e.g.
    /// "ABC1" vs "abc1") could both pass the app-level check and both insert successfully, since the DB
    /// index doesn't consider them duplicates - normalizing here means they always collide as the exact
    /// same string, so the index (and the race-condition fallback in GlobalExceptionHandler) actually
    /// catches it.</summary>
    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private async Task EnsureCodeIsUniqueAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();

        var duplicateExists = await _productRepository.ExistsAsync(
            product => product.Code.ToLower() == normalizedCode && (excludeId == null || product.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A product with code '{code}' already exists.");
        }
    }

    private async Task<string> NormalizeAndValidateBarcodeAsync(
        string barcode, long? excludeId, CancellationToken cancellationToken)
    {
        var trimmed = barcode.Trim();

        var duplicateExists = await _productRepository.ExistsAsync(
            product => product.Barcode == trimmed && (excludeId == null || product.Id != excludeId),
            cancellationToken);

        if (duplicateExists)
        {
            throw new BusinessRuleException($"A product with barcode '{trimmed}' already exists.");
        }

        return trimmed;
    }

    private async Task<string> GenerateUniqueBarcodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxBarcodeGenerationAttempts; attempt++)
        {
            var candidate = Ean13BarcodeGenerator.Generate();

            if (!await _productRepository.ExistsAsync(p => p.Barcode == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Could not generate a unique barcode after {MaxBarcodeGenerationAttempts} attempts.");
    }
}

/// <summary>Row-level parse/validation failure inside ProductService.ImportAsync - always caught within
/// that same method and folded into ProductImportRowErrorDto, never bubbles to the global exception
/// handler.</summary>
internal sealed class ProductImportRowException(string message) : Exception(message);
