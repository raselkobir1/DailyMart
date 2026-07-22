using DailyMart.Application.AuditLogs;
using DailyMart.Application.Auth;
using DailyMart.Application.Customers;
using DailyMart.Application.Inventory;
using DailyMart.Application.MasterData;
using DailyMart.Application.Products;
using DailyMart.Application.Purchases;
using DailyMart.Application.Settings;
using DailyMart.Application.Suppliers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DailyMart.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IShopSettingsService, ShopSettingsService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IUnitService, UnitService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
