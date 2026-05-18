using System.Text;
using System.Text.Json;
using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Core.Messaging;
using IntegrationService.Api.Interfaces;
using IntegrationService.Api.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IntegrationService.Api.Consumers;

/// <summary>
/// RabbitMQ Consumer — lắng nghe queue "integration.requests".
/// Khi nhận message → gọi IntegrationExecutor → publish kết quả lên "integration.results".
/// 
/// Flow async:
/// App A → publish message lên "integration.requests"
/// Integration Service consume → gọi third-party → publish result lên "integration.results"
/// App A consume result (hoặc nhận qua WebSocket)
/// </summary>
public class IntegrationMessageConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<IntegrationMessageConsumer> _logger;

    private const string RequestQueue = "integration.requests";
    private const string ResultExchange = "integration.results";

    public IntegrationMessageConsumer(
        IServiceProvider serviceProvider,
        IConnection connection,
        IMessagePublisher publisher,
        ILogger<IntegrationMessageConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(RequestQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.ExchangeDeclareAsync(ResultExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 5, false, stoppingToken); // prefetch 5

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<IntegrationMessage>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (message == null)
                {
                    _logger.LogWarning("Invalid message received, skipping");
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                _logger.LogInformation(
                    "Processing async integration: Config={Config} Op={Op} MsgId={MsgId} CorrId={CorrId}",
                    message.ConnectorConfigId, message.Operation, message.MessageId, message.CorrelationId);

                // Gọi IntegrationExecutor (cùng logic với HTTP endpoint)
                using var scope = _serviceProvider.CreateScope();
                var executor = scope.ServiceProvider.GetRequiredService<IIntegrationExecutor>();

                var request = new IntegrationRequest
                {
                    ConnectorConfigId = message.ConnectorConfigId,
                    Operation = message.Operation,
                    Data = message.Data,
                    Headers = message.Headers,
                    IdempotencyKey = message.IdempotencyKey
                };

                var response = await executor.ExecuteAsync(request, message.CorrelationId, CancellationToken.None);

                // Publish kết quả
                var result = new IntegrationResultMessage
                {
                    MessageId = message.MessageId,
                    CorrelationId = message.CorrelationId,
                    Success = response.Success,
                    Data = response.Data,
                    Error = response.ErrorMessage,
                    ErrorCode = response.ErrorCode,
                    ExecutionTimeMs = response.ExecutionTimeMs
                };

                // Publish lên exchange (topic routing)
                var routingKey = $"result.{message.ConnectorConfigId}.{message.Operation}";
                await _publisher.PublishAsync(ResultExchange, routingKey, result);

                // Nếu có ReplyTo → publish thêm vào queue riêng
                if (!string.IsNullOrEmpty(message.ReplyTo))
                    await _publisher.PublishToQueueAsync(message.ReplyTo, result);

                _logger.LogInformation(
                    "Async integration completed: Config={Config} Success={Success} Time={Time}ms",
                    message.ConnectorConfigId, response.Success, response.ExecutionTimeMs);

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");
                // Nack + requeue (retry)
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await channel.BasicConsumeAsync(RequestQueue, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("RabbitMQ consumer started on queue: {Queue}", RequestQueue);

        // Keep running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
