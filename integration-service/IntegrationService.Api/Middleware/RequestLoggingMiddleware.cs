using System.Diagnostics;

namespace IntegrationService.Api.Middleware;

/// <summary>
/// Middleware: correlation ID propagation + request/response logging + timing.
/// Production-grade: structured logging, không log sensitive data.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Correlation ID
        if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
            context.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString();

        var correlationId = context.Request.Headers["X-Correlation-Id"].ToString();
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrEmpty(traceId))
            context.Response.Headers["X-Trace-Id"] = traceId;

        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
            sw.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} → {StatusCode} in {Ms}ms | CorrelationId={CorrId}",
                context.Request.Method, context.Request.Path,
                context.Response.StatusCode, sw.ElapsedMilliseconds, correlationId);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "HTTP {Method} {Path} → EXCEPTION in {Ms}ms | CorrelationId={CorrId}",
                context.Request.Method, context.Request.Path,
                sw.ElapsedMilliseconds, correlationId);
            throw;
        }
    }
}
