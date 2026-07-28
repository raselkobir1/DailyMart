using DailyMart.Application.Common.Interfaces;
using DailyMart.Domain.Auditing;
using DailyMart.Domain.Auth;
using DailyMart.Domain.Billing;
using DailyMart.Domain.Customers;
using DailyMart.Domain.Expenses;
using DailyMart.Domain.Inventory;
using DailyMart.Domain.MasterData;
using DailyMart.Domain.Products;
using DailyMart.Domain.Purchases;
using DailyMart.Domain.Rbac;
using DailyMart.Domain.Sales;
using DailyMart.Domain.Settings;
using DailyMart.Domain.Suppliers;
using DailyMart.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace DailyMart.Infrastructure.Persistence;

public class DailyMartDbContext : DbContext, ITenantScopedDbContext
{
    private readonly ICurrentTenantService _currentTenantService;

    public DailyMartDbContext(DbContextOptions<DailyMartDbContext> options, ICurrentTenantService currentTenantService)
        : base(options)
    {
        _currentTenantService = currentTenantService;
    }

    /// <summary>Read by the tenant query filter (see TenancyModelExtensions) - exposed as a property
    /// on the DbContext itself, not just consumed inline, so the filter expression can capture this
    /// DbContext instance and re-evaluate it per query rather than baking in a stale value from
    /// whenever the model was first built.</summary>
    public long? CurrentTenantId => _currentTenantService.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Menu> Menus => Set<Menu>();

    public DbSet<RoleMenuPermission> RoleMenuPermissions => Set<RoleMenuPermission>();

    public DbSet<TenantFeatureGrant> TenantFeatureGrants => Set<TenantFeatureGrant>();

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

    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DailyMartDbContext).Assembly);

        // Every TenantOwnedEntity gets an FK to Tenant plus a combined soft-delete + tenant-isolation
        // query filter, applied by convention rather than repeated per module (see CLAUDE.md §4 and
        // TenancyModelExtensions' doc comment for why this replaces the plain soft-delete filter here).
        modelBuilder.ApplyTenantForeignKeys();
        modelBuilder.ApplyTenancyQueryFilters(this);

        base.OnModelCreating(modelBuilder);
    }
}
