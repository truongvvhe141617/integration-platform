using ConfigService.Application.Contracts;
using ConfigService.Application.Services;
using ConfigService.Domain.Repositories;
using ConfigService.Infrastructure.Data;
using ConfigService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigService.Infrastructure;

/// <summary>
/// Tất cả DI infrastructure đăng ký tại đây.
/// Program.cs chỉ gọi: builder.Services.AddInfrastructureServices(configuration)
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ──
        var connectionString = configuration.GetConnectionString("SqlServer");
        var timeoutStr = configuration["ConnectionStrings:TimeOut"];
        var retryStr = configuration["ConnectionStrings:SqlRetry"];
        var timeoutSeconds = int.TryParse(timeoutStr, out var t) ? t : 180;
        var sqlRetry = int.TryParse(retryStr, out var r) ? r : 5;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(timeoutSeconds);
                    sql.EnableRetryOnFailure(sqlRetry, TimeSpan.FromSeconds(5), null);
                }));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("ConfigServiceDb"));
        }

        // ── Repositories (Domain layer — CQRS) ──
        services.AddScoped<IIntegrationConfigRepository, IntegrationConfigRepository>();

        // ── Repositories (Legacy — backward compatible for Integration Service) ──
        services.AddSingleton<IConfigRepository, InMemoryConfigRepository>();
        services.AddSingleton<IConfigAuditRepository, InMemoryConfigAuditRepository>();

        // ── Application Services ──
        services.AddScoped<IConfigService, ConfigManagementService>();

        // ── Cache ──
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
        else
            services.AddDistributedMemoryCache();

        return services;
    }
}
