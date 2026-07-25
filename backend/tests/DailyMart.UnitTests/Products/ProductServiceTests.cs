using System.Linq;
using System.Linq.Expressions;
using ClosedXML.Excel;
using DailyMart.Application.Common.Exceptions;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Products;
using DailyMart.Domain.MasterData;
using DailyMart.Domain.Products;
using Moq;

namespace DailyMart.UnitTests.Products;

public class ProductServiceTests
{
    private const long CategoryId = 1;
    private const long BrandId = 2;
    private const long UnitId = 3;

    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IRepository<Category>> _categoryRepository = new();
    private readonly Mock<IRepository<Brand>> _brandRepository = new();
    private readonly Mock<IRepository<Unit>> _unitRepository = new();
    private readonly Mock<IFileStorageService> _fileStorageService = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _unitOfWork.Setup(u => u.Repository<Category>()).Returns(_categoryRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Brand>()).Returns(_brandRepository.Object);
        _unitOfWork.Setup(u => u.Repository<Unit>()).Returns(_unitRepository.Object);

        _categoryRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _brandRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _unitRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Unit, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _categoryRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Category { Id = CategoryId, Name = "Grocery" }]);
        _brandRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Brand { Id = BrandId, Name = "Nestle" }]);
        _unitRepository
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Unit, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Unit { Id = UnitId, Name = "Kilogram", Symbol = "kg" }]);

        _categoryRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Category { Id = CategoryId, Name = "Grocery" }]);
        _brandRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Brand { Id = BrandId, Name = "Nestle" }]);
        _unitRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Unit { Id = UnitId, Name = "Kilogram", Symbol = "kg" }]);
        _productRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Default: nothing is ever a duplicate. Individual tests override this to simulate collisions.
        _productRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _sut = new ProductService(
            _productRepository.Object,
            _unitOfWork.Object,
            _fileStorageService.Object,
            new CreateProductRequestValidator(),
            new ProductRequestValidator());
    }

    private static CreateProductRequestDto ValidCreateRequest(
        string code = "P-001",
        string? barcode = null,
        decimal purchasePrice = 50,
        decimal sellingPrice = 60,
        bool allowPriceBelowCost = false,
        decimal currentStock = 10) => new()
    {
        Code = code,
        Barcode = barcode,
        Name = "Rice 1kg",
        CategoryId = CategoryId,
        BrandId = BrandId,
        UnitId = UnitId,
        PurchasePrice = purchasePrice,
        SellingPrice = sellingPrice,
        DiscountPercentage = 0,
        TaxPercentage = 0,
        MinimumStock = 5,
        AllowPriceBelowCost = allowPriceBelowCost,
        CurrentStock = currentStock
    };

    private static ProductRequestDto ValidUpdateRequest(
        string code = "P-001",
        string? barcode = null,
        decimal purchasePrice = 50,
        decimal sellingPrice = 60,
        bool allowPriceBelowCost = false) => new()
    {
        Code = code,
        Barcode = barcode,
        Name = "Rice 1kg",
        CategoryId = CategoryId,
        BrandId = BrandId,
        UnitId = UnitId,
        PurchasePrice = purchasePrice,
        SellingPrice = sellingPrice,
        DiscountPercentage = 0,
        TaxPercentage = 0,
        MinimumStock = 5,
        AllowPriceBelowCost = allowPriceBelowCost
    };

    private static bool IsValidEan13(string barcode)
    {
        if (barcode.Length != 13 || !barcode.All(char.IsDigit))
        {
            return false;
        }

        var digits = barcode.Select(c => c - '0').ToArray();
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += digits[i] * (i % 2 == 0 ? 1 : 3);
        }

        return (10 - (sum % 10)) % 10 == digits[12];
    }

    [Fact]
    public async Task GetPagedAsync_resolves_category_brand_and_unit_names()
    {
        _productRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Product, bool>>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Product>
            {
                Items = [new Product
                {
                    Id = 1, Code = "P-001", Barcode = "123", Name = "Rice 1kg",
                    CategoryId = CategoryId, BrandId = BrandId, UnitId = UnitId,
                    PurchasePrice = 50, SellingPrice = 60
                }],
                TotalCount = 1,
                PageNumber = 1,
                PageSize = 20
            });

        var result = await _sut.GetPagedAsync(new PagedRequest());

        var dto = Assert.Single(result.Items);
        Assert.Equal("Grocery", dto.CategoryName);
        Assert.Equal("Nestle", dto.BrandName);
        Assert.Equal("Kilogram", dto.UnitName);
        Assert.Equal("kg", dto.UnitSymbol);
    }

    [Fact]
    public async Task GetLowStockAsync_filters_products_where_CurrentStock_is_at_or_below_MinimumStock()
    {
        Expression<Func<Product, bool>>? capturedPredicate = null;
        _productRepository
            .Setup(r => r.GetPagedAsync(It.IsAny<PagedRequest>(), It.IsAny<Expression<Func<Product, bool>>?>(), It.IsAny<CancellationToken>()))
            .Callback<PagedRequest, Expression<Func<Product, bool>>?, CancellationToken>((_, predicate, _) => capturedPredicate = predicate)
            .ReturnsAsync(new PagedResult<Product> { Items = [], TotalCount = 0, PageNumber = 1, PageSize = 20 });

        await _sut.GetLowStockAsync(new PagedRequest());

        Assert.NotNull(capturedPredicate);
        var compiled = capturedPredicate!.Compile();

        Assert.True(compiled(new Product { CurrentStock = 2, MinimumStock = 5 }));
        Assert.True(compiled(new Product { CurrentStock = 5, MinimumStock = 5 }));
        Assert.False(compiled(new Product { CurrentStock = 8, MinimumStock = 5 }));
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFoundException_when_missing()
    {
        _productRepository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByBarcodeAsync_throws_NotFoundException_when_missing()
    {
        _productRepository
            .Setup(r => r.GetByBarcodeAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByBarcodeAsync("nope"));
    }

    [Fact]
    public async Task CreateAsync_with_an_unknown_category_throws_BusinessRuleException()
    {
        _categoryRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Category, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidCreateRequest()));

        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_with_an_unknown_unit_throws_BusinessRuleException()
    {
        _unitRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Unit, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidCreateRequest()));
    }

    [Fact]
    public async Task CreateAsync_with_an_unknown_brand_throws_BusinessRuleException()
    {
        _brandRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Brand, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidCreateRequest()));
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_code()
    {
        _productRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidCreateRequest()));

        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_duplicate_supplied_barcode_while_the_code_stays_unique()
    {
        const string barcode = "1234567890128";

        // A single sentinel product simulates the DB: its Code differs from the request's (so the
        // code-uniqueness predicate is false against it) but its Barcode matches (so that predicate is
        // true) - proving the service can tell the two checks apart, not just that "some" check failed.
        _productRepository
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Product, bool>> predicate, CancellationToken _) =>
                predicate.Compile().Invoke(new Product { Code = "SOME-OTHER-CODE", Barcode = barcode }));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(ValidCreateRequest(barcode: barcode)));

        _productRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_without_a_barcode_generates_a_valid_EAN13_prefixed_with_20()
    {
        Product? captured = null;
        _productRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(ValidCreateRequest(barcode: null));

        Assert.NotNull(captured);
        Assert.StartsWith("20", captured!.Barcode);
        Assert.True(IsValidEan13(captured.Barcode), $"'{captured.Barcode}' is not a valid EAN-13 value.");
    }

    [Fact]
    public async Task CreateAsync_rejects_selling_price_below_purchase_price_by_default()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.CreateAsync(ValidCreateRequest(purchasePrice: 100, sellingPrice: 90)));
    }

    [Fact]
    public async Task CreateAsync_allows_selling_price_below_purchase_price_when_explicitly_allowed()
    {
        var result = await _sut.CreateAsync(
            ValidCreateRequest(purchasePrice: 100, sellingPrice: 90, allowPriceBelowCost: true));

        Assert.Equal(90, result.SellingPrice);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFoundException_when_missing()
    {
        _productRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(404, ValidUpdateRequest()));
    }

    [Fact]
    public async Task UpdateAsync_never_changes_CurrentStock()
    {
        var existing = new Product
        {
            Id = 5, Code = "P-001", Barcode = "123", Name = "Rice 1kg",
            CategoryId = CategoryId, BrandId = BrandId, UnitId = UnitId,
            PurchasePrice = 50, SellingPrice = 60, CurrentStock = 42
        };
        _productRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(5, ValidUpdateRequest());

        Assert.Equal(42, result.CurrentStock);
        Assert.Equal(42, existing.CurrentStock);
    }

    [Fact]
    public async Task UpdateAsync_keeps_the_existing_barcode_when_the_request_barcode_is_blank()
    {
        var existing = new Product
        {
            Id = 5, Code = "P-001", Barcode = "9990001112223", Name = "Rice 1kg",
            CategoryId = CategoryId, BrandId = BrandId, UnitId = UnitId,
            PurchasePrice = 50, SellingPrice = 60
        };
        _productRepository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.UpdateAsync(5, ValidUpdateRequest(barcode: null));

        Assert.Equal("9990001112223", result.Barcode);
    }

    [Fact]
    public async Task DeleteAsync_removes_and_saves_when_the_product_exists()
    {
        var existing = new Product { Id = 7, Code = "P-007", Barcode = "1", CategoryId = CategoryId, UnitId = UnitId };
        _productRepository.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        await _sut.DeleteAsync(7);

        _productRepository.Verify(r => r.Remove(existing), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFoundException_when_missing()
    {
        _productRepository.Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(404));
    }

    [Fact]
    public async Task UploadImageAsync_rejects_an_unsupported_extension_without_touching_storage()
    {
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UploadImageAsync(1, content, "photo.gif"));

        _fileStorageService.Verify(
            f => f.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadImageAsync_rejects_a_file_over_the_size_cap_without_touching_storage()
    {
        using var content = new MemoryStream(new byte[2 * 1024 * 1024 + 1]);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UploadImageAsync(1, content, "photo.png"));

        _fileStorageService.Verify(
            f => f.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UploadImageAsync_with_a_valid_file_updates_ImageUrl()
    {
        var existing = new Product
        {
            Id = 1, Code = "P-001", Barcode = "123", CategoryId = CategoryId, BrandId = BrandId, UnitId = UnitId
        };
        _productRepository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _fileStorageService
            .Setup(f => f.SaveAsync(It.IsAny<Stream>(), "photo.png", "products", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/uploads/products/new.png");

        using var content = new MemoryStream([1, 2, 3]);

        var result = await _sut.UploadImageAsync(1, content, "photo.png");

        Assert.Equal("/uploads/products/new.png", result.ImageUrl);
        Assert.Equal("/uploads/products/new.png", existing.ImageUrl);
    }

    [Fact]
    public async Task ImportAsync_creates_new_rows_and_updates_rows_whose_code_already_exists()
    {
        var existingProduct = new Product
        {
            Id = 42, Code = "P-001", Barcode = "1112223334445", Name = "Old Name",
            CategoryId = CategoryId, BrandId = BrandId, UnitId = UnitId,
            PurchasePrice = 10, SellingPrice = 20, CurrentStock = 99
        };
        _productRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([existingProduct]);
        _productRepository.Setup(r => r.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(existingProduct);

        using var workbook = BuildImportWorkbook(
            new object?[] { "P-001", null, "Rice 1kg Updated", "Grocery", "Nestle", "Kilogram", 50m, 60m, null, null, null, 5m, 5m, null },
            new object?[] { "IMP-NEW-1", null, "New Product", "Grocery", "Nestle", "Kilogram", 30m, 45m, null, null, null, 20m, 5m, null });

        var result = await _sut.ImportAsync(workbook);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Updated);
        Assert.Empty(result.Errors);
        Assert.Equal("Rice 1kg Updated", existingProduct.Name);
        Assert.Equal(99, existingProduct.CurrentStock);
        _productRepository.Verify(
            r => r.AddAsync(It.Is<Product>(p => p.Code == "IMP-NEW-1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportAsync_records_a_row_error_for_an_unknown_category_without_aborting_other_rows()
    {
        using var workbook = BuildImportWorkbook(
            new object?[] { "BAD-1", null, "Bad Row", "NoSuchCategory", null, "Kilogram", 10m, 20m, null, null, null, 5m, 5m, null },
            new object?[] { "GOOD-1", null, "Good Row", "Grocery", "Nestle", "Kilogram", 10m, 20m, null, null, null, 5m, 5m, null });

        var result = await _sut.ImportAsync(workbook);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.Created);
        var error = Assert.Single(result.Errors);
        Assert.Equal(2, error.RowNumber);
        Assert.Contains("NoSuchCategory", error.Message);
    }

    [Fact]
    public async Task ImportAsync_throws_BusinessRuleException_when_a_required_column_is_missing()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");
        sheet.Cell(1, 1).Value = "Code";
        sheet.Cell(1, 2).Value = "Name";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ImportAsync(stream));
    }

    private static MemoryStream BuildImportWorkbook(params object?[][] rows)
    {
        string[] headers =
        [
            "Code", "Barcode", "Name", "Category", "Brand", "Unit", "PurchasePrice", "SellingPrice",
            "WholesalePrice", "DiscountPercentage", "TaxPercentage", "CurrentStock", "MinimumStock",
            "AllowPriceBelowCost"
        ];

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Products");

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                switch (rows[r][c])
                {
                    case null:
                        break;
                    case string s:
                        sheet.Cell(r + 2, c + 1).Value = s;
                        break;
                    case decimal d:
                        sheet.Cell(r + 2, c + 1).Value = (double)d;
                        break;
                }
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
