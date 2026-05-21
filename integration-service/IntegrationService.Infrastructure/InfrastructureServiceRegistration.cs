using IntegrationService.Application.Contracts;
using IntegrationService.Domain.Contracts.Persistence;
using IntegrationService.Infrastructure.Persistence;
using IntegrationService.Infrastructure.Repositories;
using IntegrationService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationService.Infrastructure;

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
            services.AddDbContext<IntegrationDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(timeoutSeconds);
                    sql.EnableRetryOnFailure(sqlRetry, TimeSpan.FromSeconds(5), null);
                }));
        }
        else
        {
            services.AddDbContext<IntegrationDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationServiceDb"));
        }

        // ── Repositories ──
        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IExecutionLogRepository, ExecutionLogRepository>();

        // ── Cache ──
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
        else
            services.AddDistributedMemoryCache();

        // ── Config Provider ──
        var configMode = configuration["Connectors:ConfigMode"] ?? "file";
        if (configMode == "file")
            services.AddSingleton<IConfigProvider, FileConfigProvider>();
        else
            services.AddScoped<IConfigProvider, ConfigProvider>();

        // ── Idempotency Store ──
        services.AddScoped<IIdempotencyStore, RedisIdempotencyStore>();

        return services;
    }
}
