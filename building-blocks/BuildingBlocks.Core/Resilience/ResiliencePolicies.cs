using System.Threading.RateLimiting;
using BuildingBlocks.Abstractions.Connectors;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using Polly.Timeout;

namespace BuildingBlocks.Core.Resilience;

/// <summary>
/// Tạo Polly resilience pipeline cho mỗi connector.
/// Pipeline: Bulkhead (RateLimiter) → Timeout → Retry → Circuit Breaker
/// </summary>
public static class ResiliencePolicies
{
    public static ResiliencePipeline<ConnectorResponse> CreatePipeline(
        ConnectorConfig config, ILogger logger)
    {
        var maxConcurrency = config.Metadata.TryGetValue("maxConcurrency", out var mc)
            ? int.Parse(mc) : 20;
        var maxQueue = config.Metadata.TryGetValue("maxQueueSize", out var mq)
            ? int.Parse(mq) : 50;

        var limiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions
        {
            PermitLimit = maxConcurrency,
            QueueLimit = maxQueue
        });

        return new ResiliencePipelineBuilder<ConnectorResponse>()
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                RateLimiter = args => limiter.AcquireAsync(1, args.Context.CancellationToken)
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs),
                OnTimeout = args =>
                {
                    logger.LogWarning("Timeout {Ms}ms connector {Id}", config.TimeoutMs, config.Id);
                    return default;
                }
            })
            .AddRetry(new RetryStrategyOptions<ConnectorResponse>
            {
                MaxRetryAttempts = config.Retry.MaxRetries,
                Delay = TimeSpan.FromMilliseconds(config.Retry.InitialDelayMs),
                BackoffType = config.Retry.BackoffStrategy.ToLowerInvariant() switch
                {
                    "exponential" => DelayBackoffType.Exponential,
                    "linear" => DelayBackoffType.Linear,
                    _ => DelayBackoffType.Constant
                },
                ShouldHandle = new PredicateBuilder<ConnectorResponse>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(r => config.Retry.RetryOnStatusCodes.Contains(r.StatusCode)),
                OnRetry = args =>
                {
                    logger.LogWarning("Retry {N}/{Max} connector {Id}",
                        args.AttemptNumber, config.Retry.MaxRetries, config.Id);
                    return default;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<ConnectorResponse>
            {
                FailureRatio = 0.5,
                MinimumThroughput = config.CircuitBreaker.FailureThreshold,
                SamplingDuration = TimeSpan.FromSeconds(config.CircuitBreaker.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(config.CircuitBreaker.DurationOfBreakSeconds),
                ShouldHandle = new PredicateBuilder<ConnectorResponse>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => !r.IsSuccess),
                OnOpened = args =>
                {
                    logger.LogError("Circuit OPENED connector {Id}", config.Id);
                    return default;
                },
                OnClosed = args =>
                {
                    logger.LogInformation("Circuit CLOSED connector {Id}", config.Id);
                    return default;
                }
            })
            .Build();
    }
}
