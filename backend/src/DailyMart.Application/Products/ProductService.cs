using System.Linq.Expressions;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Domain.MasterData;
using DailyMart.Domain.Products;

namespace DailyMart.Application.Products;

public class ProductService : IProductService
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private const long MaxImageSizeBytes = 2 * 1024 * 1024;
    private const int MaxBarcodeGenerationAttempts = 5;

    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;

    public ProductService(
        IProductRepository productRepository, IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;
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
