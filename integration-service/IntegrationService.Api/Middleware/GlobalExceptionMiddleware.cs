using System.Text.Json;

namespace IntegrationService.Api.Middleware;

/// <summary>
/// Global exception handler — catch unhandled exceptions, trả JSON error response.
/// Production: không leak stack trace, log đầy đủ server-side.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly bool _isDevelopment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _isDevelopment = env.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = new
            {
                success = false,
                error = _isDevelopment ? ex.Message : "Internal server error",
                errorCode = "INTERNAL_ERROR",
                correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? "",
                // Production: không trả stack trace
                stackTrace = _isDevelopment ? ex.StackTrace : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
