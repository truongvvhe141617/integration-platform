using System.Diagnostics;
using BuildingBlocks.Abstractions.Messaging;
using BuildingBlocks.Core.Messaging;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class IntegrationController : ControllerBase
{
    private readonly IIntegrationExecutor _executor;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<IntegrationController> _logger;

    public IntegrationController(
        IIntegrationExecutor executor, IMessagePublisher publisher,
        ILogger<IntegrationController> logger)
    {
        _executor = executor;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>Sync: gọi third-party, đợi response</summary>
    [HttpPost("execute")]
    public async Task<IActionResult> Execute(
        [FromBody] IntegrationRequest request, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault() ?? Guid.NewGuid().ToString();

        var lang = HttpContext.Request.Headers["Accept-Language"].FirstOrDefault();
        if (!string.IsNullOrEmpty(lang))
        {
            request.Headers ??= new();
            request.Headers["Accept-Language"] = lang;
        }

        HttpContext.Response.Headers["X-Correlation-Id"] = correlationId;
        HttpContext.Response.Headers["X-Trace-Id"] = Activity.Current?.TraceId.ToString() ?? "";

        var response = await _executor.ExecuteAsync(request, correlationId, cancellationToken);
        return StatusCode(response.HttpStatus, response);
    }

    /// <summary>Async: publish lên RabbitMQ, trả 202 ngay</summary>
    [HttpPost("execute-async")]
    public async Task<IActionResult> ExecuteAsync([FromBody] AsyncIntegrationRequest request)
    {
        var correlationId = HttpContext.Request.Headers["X-Correlation-Id"]
            .FirstOrDefault() ?? Guid.NewGuid().ToString();

        var message = new IntegrationMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId,
            ConnectorConfigId = request.ConnectorConfigId,
            Operation = request.Operation,
            Data = request.Data,
            Headers = request.Headers,
            IdempotencyKey = request.IdempotencyKey,
            ReplyTo = request.ReplyTo
        };

        await _publisher.PublishToQueueAsync("integration.requests", message);

        return StatusCode(202, new
        {
            success = true,
            httpStatus = 202,
            messageId = message.MessageId,
            correlationId,
            status = "QUEUED"
        });
    }

    [HttpGet("health/{configId}")]
    public async Task<IActionResult> HealthCheck(string configId, CancellationToken cancellationToken)
    {
        var result = await _executor.CheckConnectorHealthAsync(configId, cancellationToken);
        return result.Healthy ? Ok(result) : StatusCode(503, result);
    }
}
