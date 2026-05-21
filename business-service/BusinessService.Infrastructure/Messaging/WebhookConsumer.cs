using System.Text;
using System.Text.Json;
using BuildingBlocks.Abstractions.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BusinessService.Infrastructure.Messaging;

/// <summary>
/// Flow 2 — Consumer phía App nội bộ (Business Service).
///
/// Subscribe exchange "webhook.inbound" với routing key "webhook.#"
/// để nhận mọi callback từ đối tác qua Integration Platform.
///
/// Routing key format: webhook.{connectorConfigId}.{operation}
/// Ví dụ:
///   webhook.ewallet-momo.payment-callback
///   webhook.bank-b-transfer.transfer-notification
///   webhook.kyc-service.verification-result
///
/// Mỗi app nội bộ có thể bind thêm routing key cụ thể để chỉ nhận
/// những webhook mình quan tâm.
/// </summary>
public class WebhookConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WebhookConsumer> _logger;

    private const string WebhookExchange = "webhook.inbound";
    private const string QueueName = "business-service.webhooks";

    public WebhookConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<WebhookConsumer> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Declare exchange và queue
        await channel.ExchangeDeclareAsync(WebhookExchange, ExchangeType.Topic, durable: true,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false,
            autoDelete: false, cancellationToken: stoppingToken);

        // Bind: nhận tất cả webhook — có thể đổi thành binding key cụ thể
        // vd: "webhook.ewallet-momo.#" để chỉ nhận webhook từ MoMo
        await channel.QueueBindAsync(QueueName, WebhookExchange, "webhook.#",
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(0, 10, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var webhook = JsonSerializer.Deserialize<InboundWebhookMessage>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (webhook != null)
                    await HandleWebhookAsync(webhook, ea.RoutingKey, stoppingToken);

                await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook message");
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true, stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
        _logger.LogInformation("WebhookConsumer started. Queue={Queue} Exchange={Exchange}",
            QueueName, WebhookExchange);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleWebhookAsync(
        InboundWebhookMessage webhook,
        string routingKey,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Webhook received from partner: ConfigId={ConfigId} Op={Op} CorrId={CorrId} " +
            "Verified={Verified} RoutingKey={RoutingKey}",
            webhook.ConnectorConfigId, webhook.Operation,
            webhook.CorrelationId, webhook.SignatureVerified, routingKey);

        // Route đến handler phù hợp theo connectorConfigId + operation
        var handled = webhook.ConnectorConfigId switch
        {
            var id when id.StartsWith("ewallet-") =>
                await HandleEWalletCallbackAsync(webhook, cancellationToken),

            var id when id.StartsWith("bank-") =>
                await HandleBankNotificationAsync(webhook, cancellationToken),

            "kyc-service" =>
                await HandleKycResultAsync(webhook, cancellationToken),

            _ => HandleUnknownWebhook(webhook)
        };

        if (!handled)
        {
            _logger.LogWarning(
                "No handler found for webhook: ConfigId={ConfigId} Op={Op}",
                webhook.ConnectorConfigId, webhook.Operation);
        }
    }

    private async Task<bool> HandleEWalletCallbackAsync(
        InboundWebhookMessage webhook, CancellationToken cancellationToken)
    {
        // TODO: Cập nhật trạng thái payment trong DB
        // ví dụ: webhook.Data["transactionId"], webhook.Data["statusCode"]
        _logger.LogInformation(
            "E-Wallet callback: Op={Op} Data={Data}",
            webhook.Operation,
            System.Text.Json.JsonSerializer.Serialize(webhook.Data));

        await Task.CompletedTask; // Thay bằng business logic thực tế
        return true;
    }

    private async Task<bool> HandleBankNotificationAsync(
        InboundWebhookMessage webhook, CancellationToken cancellationToken)
    {
        // TODO: Cập nhật trạng thái chuyển khoản
        _logger.LogInformation(
            "Bank notification: Op={Op} Data={Data}",
            webhook.Operation,
            System.Text.Json.JsonSerializer.Serialize(webhook.Data));

        await Task.CompletedTask;
        return true;
    }

    private async Task<bool> HandleKycResultAsync(
        InboundWebhookMessage webhook, CancellationToken cancellationToken)
    {
        // TODO: Cập nhật kết quả KYC của user
        _logger.LogInformation(
            "KYC result received: Op={Op} Data={Data}",
            webhook.Operation,
            System.Text.Json.JsonSerializer.Serialize(webhook.Data));

        await Task.CompletedTask;
        return true;
    }

    private bool HandleUnknownWebhook(InboundWebhookMessage webhook)
    {
        _logger.LogWarning(
            "Unknown webhook connector: {ConfigId}", webhook.ConnectorConfigId);
        return false;
    }
}
