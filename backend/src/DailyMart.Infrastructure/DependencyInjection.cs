using DailyMart.Application.Auth;
using DailyMart.Application.Common.Interfaces;
using DailyMart.Application.Common.Options;
using DailyMart.Application.Products;
using DailyMart.Application.Settings;
using DailyMart.Domain.Auth;
using DailyMart.Infrastructure.Auth;
using DailyMart.Infrastructure.Files;
using DailyMart.Infrastructure.Persistence;
using DailyMart.Infrastructure.Persistence.Interceptors;
using DailyMart.Infrastructure.Persistence.Repositories;
using DailyMart.Infrastructure.Persistence.Seed;
using DailyMart.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DailyMart.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<AuditingSaveChangesInterceptor>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        // PasswordHasher<TUser> is stateless/thread-safe - singleton is the standard registration for it.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        // Explicit factory: UnitOfWork depends on the DbContext base class (so it - and Repository<T> -
        // stay reusable/testable against any DbContext), but only DailyMartDbContext is registered with
        // DI, so constructor auto-resolution can't find a plain DbContext on its own.
        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(sp.GetRequiredService<DailyMartDbContext>()));

        // Module-specific repositories are injected directly rather than fetched via
        // IUnitOfWork.Repository<T>() - that method is constrained to the generic IRepository<T>
        // contract and can't return a specialized interface like IUserRepository. Same DbContext
        // instance per scope, so SaveChangesAsync() through IUnitOfWork still commits their changes too.
        services.AddScoped<IUserRepository>(sp => new UserRepository(sp.GetRequiredService<DailyMartDbContext>()));
        services.AddScoped<IRefreshTokenRepository>(sp => new RefreshTokenRepository(sp.GetRequiredService<DailyMartDbContext>()));
        services.AddScoped<IShopSettingsRepository>(sp => new ShopSettingsRepository(sp.GetRequiredService<DailyMartDbContext>()));
        services.AddScoped<IProductRepository>(sp => new ProductRepository(sp.GetRequiredService<DailyMartDbContext>()));

        services.AddScoped<AdminSeeder>();
        services.AddScoped<ShopSettingsSeeder>();
        services.AddScoped<RbacSeeder>();

        services.AddDbContext<DailyMartDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>());
        });

        return services;
    }
}
