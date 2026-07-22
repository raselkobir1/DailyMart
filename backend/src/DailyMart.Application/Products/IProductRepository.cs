using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Products;

namespace DailyMart.Application.Products;

public interface IProductRepository : IRepository<Product>
{
    /// <summary>Exact-match lookup - what the POS barcode-scanner workflow (Module 9) will call.</summary>
    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
