using DailyMart.Application.Products;
using DailyMart.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(DbContext context) : base(context)
    {
    }

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(p => p.Barcode == barcode, cancellationToken);

    public Task<Product?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Entities.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
}
