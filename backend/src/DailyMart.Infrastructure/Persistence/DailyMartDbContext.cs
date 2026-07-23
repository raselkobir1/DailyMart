using DailyMart.Domain.Auditing;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.MasterData;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Sales;
using DailyMart.Domain.Settings;
using DailyMart.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence;

public class DailyMartDbContext : DbContext
{
    public DailyMartDbContext(DbContextOptions<DailyMartDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ShopSettings> ShopSettings => Set<ShopSettings>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<SupplierLedgerEntry> SupplierLedgerEntries => Set<SupplierLedgerEntry>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerLedgerEntry> CustomerLedgerEntries => Set<CustomerLedgerEntry>();

    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    public DbSet<PurchaseReturnItem> PurchaseReturnItems => Set<PurchaseReturnItem>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();

    public DbSet<SaleReturnItem> SaleReturnItems => Set<SaleReturnItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DailyMartDbContext).Assembly);

        // Every AuditableEntity gets a soft-delete query filter for free, applied by convention
        // rather than repeated per module (see CLAUDE.md §4).
        modelBuilder.ApplySoftDeleteQueryFilter();

        base.OnModelCreating(modelBuilder);
    }
}
