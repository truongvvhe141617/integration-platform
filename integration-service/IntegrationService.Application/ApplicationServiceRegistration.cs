using System.Reflection;
using FluentValidation;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationService.Application;

/// <summary>
/// Tất cả DI application đăng ký tại đây.
/// Program.cs chỉ gọi: builder.Services.AddApplicationServices(configuration)
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // MediatR — CQRS
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // ── Application Services ──
        services.AddScoped<IIntegrationExecutor, IntegrationExecutor>();

        return services;
    }
}
