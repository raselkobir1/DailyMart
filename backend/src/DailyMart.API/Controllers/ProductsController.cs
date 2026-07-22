using System.Globalization;
using System.Text;
using DailyMart.Application.Common.Models;
using DailyMart.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace DailyMart.API.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPaged(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetPagedAsync(request, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetByIdAsync(id, cancellationToken));
    }

    /// <summary>What the POS barcode-scanner workflow (Module 9) will call.</summary>
    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<ProductDto>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetByBarcodeAsync(barcode, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(
        CreateProductRequestDto request, CancellationToken cancellationToken)
    {
        var product = await _productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProductDto>> Update(
        long id, ProductRequestDto request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _productService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/image")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ProductDto>> UploadImage(
        long id, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        await using var stream = file.OpenReadStream();
        var product = await _productService.UploadImageAsync(id, stream, file.FileName, cancellationToken);

        return Ok(product);
    }

    /// <summary>Products at or below their configured MinimumStock - added in Module 8 (Inventory), but
    /// lives here since it only queries Product; see IProductService.GetLowStockAsync.</summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetLowStock(
        [FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _productService.GetLowStockAsync(request, cancellationToken));
    }

    /// <summary>Export only - see Module 4 Step 1's scope decision on why Import is a deferred fast-follow.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllForExportAsync(cancellationToken);
        var csvBytes = Encoding.UTF8.GetBytes(BuildCsv(products));

        return File(csvBytes, "text/csv", $"products-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static string BuildCsv(IReadOnlyList<ProductDto> products)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "Code,Barcode,Name,Category,Brand,Unit,PurchasePrice,SellingPrice,WholesalePrice," +
            "DiscountPercentage,TaxPercentage,CurrentStock,MinimumStock");

        foreach (var product in products)
        {
            var fields = new[]
            {
                EscapeCsvField(product.Code),
                EscapeCsvField(product.Barcode),
                EscapeCsvField(product.Name),
                EscapeCsvField(product.CategoryName),
                EscapeCsvField(product.BrandName ?? string.Empty),
                EscapeCsvField($"{product.UnitName} ({product.UnitSymbol})"),
                product.PurchasePrice.ToString(CultureInfo.InvariantCulture),
                product.SellingPrice.ToString(CultureInfo.InvariantCulture),
                product.WholesalePrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                product.DiscountPercentage.ToString(CultureInfo.InvariantCulture),
                product.TaxPercentage.ToString(CultureInfo.InvariantCulture),
                product.CurrentStock.ToString(CultureInfo.InvariantCulture),
                product.MinimumStock.ToString(CultureInfo.InvariantCulture)
            };

            builder.AppendLine(string.Join(',', fields));
        }

        return builder.ToString();
    }

    private static string EscapeCsvField(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
