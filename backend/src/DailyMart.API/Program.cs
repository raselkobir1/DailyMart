using System.Text;
using DailyMart.API.ExceptionHandling;
using DailyMart.API.Filters;
using DailyMart.Application;
using DailyMart.Application.Common.Options;
using DailyMart.Infrastructure;
using DailyMart.Infrastructure.Persistence;
using DailyMart.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/dailymart-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // Add services to the container.

    builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the access token returned by /api/auth/login."
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // The Angular app runs on its own origin (ng serve on :4200) against this API (:5299 in dev) - without
    // an explicit CORS policy every browser request is blocked by the browser itself, silently, before it
    // ever reaches a controller. No dev proxy exists to route around this instead, so this is required for
    // the UI to function at all, not just a nice-to-have.
    var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCorsPolicy", policy =>
        {
            if (corsAllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsAllowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    // [Authorize] by default (CLAUDE.md §4) - every endpoint requires a valid access token unless it's
    // explicitly marked [AllowAnonymous] (login/refresh/logout).
    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        // Applies any pending EF Core migrations before anything else runs - so `docker-compose up` on a
        // fresh machine needs no manual `dotnet ef database update` step. Retries with a short backoff:
        // Postgres's healthcheck reports "accepting connections" slightly before it's actually ready for
        // every kind of query on a cold volume, and this is cheap insurance against that race.
        var dbContext = scope.ServiceProvider.GetRequiredService<DailyMartDbContext>();
        await MigrateWithRetryAsync(dbContext, app.Logger);

        var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
        await adminSeeder.SeedAsync();

        var shopSettingsSeeder = scope.ServiceProvider.GetRequiredService<ShopSettingsSeeder>();
        await shopSettingsSeeder.SeedAsync();

        // Runs after AdminSeeder (needs the seeded admin's Role="Admin" to line up with the Role row it
        // creates) and on every boot, not just once - see RbacSeeder's doc comment.
        var rbacSeeder = scope.ServiceProvider.GetRequiredService<RbacSeeder>();
        await rbacSeeder.SeedAsync();
    }

    app.UseExceptionHandler();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Serves whatever LocalFileStorageService saves under wwwroot/uploads (e.g. the shop logo) back out
    // at the relative URL that gets stored in ShopSettings.ShopLogoUrl.
    app.UseStaticFiles();

    app.UseCors("DefaultCorsPolicy");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();

    static async Task MigrateWithRetryAsync(DailyMartDbContext dbContext, Microsoft.Extensions.Logging.ILogger logger)
    {
        const int maxAttempts = 10;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(ex,
                    "Database not ready yet (attempt {Attempt}/{MaxAttempts}) - retrying in 3s...",
                    attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is how `dotnet ef` design-time tooling (migrations, etc.) extracts a
    // configured host without actually running it - not a real startup failure, so it's excluded here
    // to avoid logging a false "terminated unexpectedly" fatal error on every migration command.
    Log.Fatal(ex, "DailyMart.API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
