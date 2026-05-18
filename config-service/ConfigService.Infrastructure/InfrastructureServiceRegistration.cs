using ConfigService.Domain.Repositories;
using ConfigService.Infrastructure.Data;
using ConfigService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigService.Infrastructure;

/// <summary>
/// Pattern từ iolis-instrument: tất cả DI infrastructure đăng ký tại đây.
/// Program.cs chỉ gọi: builder.Services.AddInfrastructureServices(configuration)
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer");
        var timeoutStr = configuration["ConnectionStrings:TimeOut"];
        var retryStr = configuration["ConnectionStrings:SqlRetry"];
        var timeoutSeconds = int.TryParse(timeoutStr, out var t) ? t : 180;
        var sqlRetry = int.TryParse(retryStr, out var r) ? r : 5;

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            // SQL Server (production)
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(timeoutSeconds);
                    sql.EnableRetryOnFailure(sqlRetry, TimeSpan.FromSeconds(5), null);
                }));
        }
        else
        {
            // InMemory fallback (dev không có SQL Server)
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("master"));
        }

        // Repositories
        services.AddScoped<IIntegrationConfigRepository, IntegrationConfigRepository>();

        // Redis cache
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
        else
            services.AddDistributedMemoryCache();

        return services;
    }
}
