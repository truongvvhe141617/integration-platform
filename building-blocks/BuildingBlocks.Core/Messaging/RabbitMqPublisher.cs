using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace BuildingBlocks.Core.Messaging;

/// <summary>
/// Publish message lên RabbitMQ.
/// Dùng chung cho tất cả services.
/// </summary>
public interface IMessagePublisher : IAsyncDisposable
{
    Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class;
    Task PublishToQueueAsync<T>(string queueName, T message) where T : class;
}

public class RabbitMqPublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message) where T : class
    {
        await _channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString(),
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, body);
        _logger.LogInformation("Published to {Exchange}/{RoutingKey}: {Type}", exchange, routingKey, typeof(T).Name);
    }

    public async Task PublishToQueueAsync<T>(string queueName, T message) where T : class
    {
        await _channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await _channel.BasicPublishAsync("", queueName, mandatory: false, props, body);
        _logger.LogInformation("Published to queue {Queue}: {Type}", queueName, typeof(T).Name);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
    }
}
