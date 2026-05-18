using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Core.Messaging;

/// <summary>
/// No-op publisher — dùng khi không có RabbitMQ.
/// Async endpoint vẫn hoạt động nhưng log warning thay vì publish.
/// </summary>
public class NoOpMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NoOpMessagePublisher> _logger;

    public NoOpMessagePublisher(ILogger<NoOpMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class
    {
        _logger.LogWarning("RabbitMQ not configured. Message to {Exchange}/{Key} dropped.", exchange, routingKey);
        return Task.CompletedTask;
    }

    public Task PublishToQueueAsync<T>(string queueName, T message) where T : class
    {
        _logger.LogWarning("RabbitMQ not configured. Message to queue {Queue} dropped.", queueName);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
