using System.Text;
using System.Text.Json;
using BuildingBlocks.Abstractions.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebSocketService.Api.Hubs;

namespace WebSocketService.Api.Consumers;

/// <summary>
/// Consume kết quả từ RabbitMQ "integration.results" → push tới WebSocket clients.
/// </summary>
public class ResultConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly ILogger<ResultConsumer> _logger;

    private const string ResultExchange = "integration.results";
    private const string QueueName = "websocket.integration.results";

    public ResultConsumer(
        IConnection connection,
        WebSocketConnectionManager wsManager,
        ILogger<ResultConsumer> logger)
    {
        _connection = connection;
        _wsManager = wsManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(ResultExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueBindAsync(QueueName, ResultExchange, "result.#", cancellationToken: stoppingToken); // Subscribe all results

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var result = JsonSerializer.Deserialize<IntegrationResultMessage>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result != null)
                {
                    _logger.LogInformation(
                        "Result received: CorrId={CorrId} Success={Success}",
                        result.CorrelationId, result.Success);

                    // Push tới WebSocket client
                    await _wsManager.SendToCorrelationAsync(result.CorrelationId, result);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing result message");
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("WebSocket result consumer started");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
