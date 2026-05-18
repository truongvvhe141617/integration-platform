using BusinessService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessService.Infrastructure;

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
            services.AddDbContext<BusinessDbContext>(options =>
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(timeoutSeconds);
                    sql.EnableRetryOnFailure(sqlRetry, TimeSpan.FromSeconds(5), null);
                }));
        }
        else
        {
            services.AddDbContext<BusinessDbContext>(options =>
                options.UseInMemoryDatabase("BusinessServiceDb"));
        }

        return services;
    }
}
