using IntegrationService.Domain.Contracts.Persistence;
using IntegrationService.Infrastructure.Persistence;
using IntegrationService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationService.Infrastructure;

/// <summary>
/// Pattern iolis-instrument: tất cả DI infrastructure đăng ký tại đây.
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
            services.AddDbContext<IntegrationDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(timeoutSeconds);
                    sql.EnableRetryOnFailure(sqlRetry, TimeSpan.FromSeconds(5), null);
                }));
        }
        else
        {
            // InMemory fallback cho dev
            services.AddDbContext<IntegrationDbContext>(options =>
                options.UseInMemoryDatabase("IntegrationServiceDb"));
        }

        // Generic repository
        services.AddScoped(typeof(IAsyncRepository<>), typeof(RepositoryBase<>));

        // Specific repositories
        services.AddScoped<IExecutionLogRepository, ExecutionLogRepository>();

        // Redis cache
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
        else
            services.AddDistributedMemoryCache();

        return services;
    }
}
