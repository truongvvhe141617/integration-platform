using ConfigService.Application.Common.Models;
using ConfigService.Application.Features.Integrations.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class IntegrationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public IntegrationsController(IMediator mediator) => _mediator = mediator;

    private Guid TenantId {
        get {
            var header = HttpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "";
            return Guid.TryParse(header, out var id) ? id : Guid.Empty;
        }
    }
    private string CurrentUser => HttpContext.User.Identity?.Name
        ?? HttpContext.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";

    /// <summary>GET /api/v1/integrations — List with pagination + filter</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] string? status,
        [FromQuery] string? tags, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetIntegrationsQuery(
            TenantId, search, status, tags, page, pageSize));
        return Ok(result);
    }

    /// <summary>GET /api/v1/integrations/:id</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetIntegrationByIdQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>GET /api/v1/integrations/by-key/:configKey</summary>
    [HttpGet("by-key/{configKey}")]
    public async Task<IActionResult> GetByKey(string configKey)
    {
        var result = await _mediator.Send(new GetIntegrationByKeyQuery(TenantId, configKey));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>POST /api/v1/integrations</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIntegrationRequest request)
    {
        var result = await _mediator.Send(new CreateIntegrationCommand(
            TenantId, request.ConfigKey, request.Name, request.Description,
            request.ConnectorType ?? "generic-http", request.BaseUrl,
            request.DefaultHeaders, request.AuthType ?? "none", request.AuthParams,
            request.TimeoutMs, request.RetryConfig, request.CircuitBreaker,
            request.Tags, request.Metadata, request.Operations ?? new(), CurrentUser));

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>PUT /api/v1/integrations/:id</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIntegrationRequest request)
    {
        var result = await _mediator.Send(new UpdateIntegrationCommand(
            id, request.Name, request.BaseUrl, request.Description,
            request.AuthType ?? "none", request.AuthParams, request.DefaultHeaders,
            request.TimeoutMs, request.RetryConfig, request.CircuitBreaker,
            request.Tags, request.Metadata, request.Operations ?? new(), CurrentUser));

        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>DELETE /api/v1/integrations/:id</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteIntegrationCommand(id, CurrentUser));
        return result.IsSuccess ? NoContent() : BadRequest(new { error = result.Error });
    }

    /// <summary>PATCH /api/v1/integrations/:id/status</summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest request)
    {
        var result = await _mediator.Send(new ChangeIntegrationStatusCommand(id, request.Status, CurrentUser));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>POST /api/v1/integrations/:id/test — Test integration</summary>
    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id, [FromBody] TestIntegrationRequest request)
    {
        var result = await _mediator.Send(new TestIntegrationCommand(id, request.Operation, request.TestData));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>GET /api/v1/integrations/:id/history</summary>
    [HttpGet("{id:guid}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var result = await _mediator.Send(new GetConfigHistoryQuery(id));
        return Ok(result);
    }

    /// <summary>GET /api/v1/integrations/:id/audit</summary>
    [HttpGet("{id:guid}/audit")]
    public async Task<IActionResult> GetAuditLog(Guid id)
    {
        var result = await _mediator.Send(new GetAuditLogQuery(id));
        return Ok(result);
    }
}

// ── Request DTOs ──
public record CreateIntegrationRequest(
    string ConfigKey, string Name, string? Description,
    string? ConnectorType, string BaseUrl, string? DefaultHeaders,
    string? AuthType, string? AuthParams, int TimeoutMs = 30000,
    string? RetryConfig = null, string? CircuitBreaker = null,
    string? Tags = null, string? Metadata = null,
    List<OperationDto>? Operations = null);

public record UpdateIntegrationRequest(
    string Name, string BaseUrl, string? Description,
    string? AuthType, string? AuthParams, string? DefaultHeaders,
    int TimeoutMs = 30000, string? RetryConfig = null,
    string? CircuitBreaker = null, string? Tags = null,
    string? Metadata = null, List<OperationDto>? Operations = null);

public record ChangeStatusRequest(string Status);
public record TestIntegrationRequest(string Operation, Dictionary<string, object> TestData);

// ── Placeholder queries (implement similarly to commands) ──
public record GetIntegrationsQuery(Guid TenantId, string? Search, string? Status,
    string? Tags, int Page, int PageSize) : IRequest<object>;
public record GetIntegrationByIdQuery(Guid Id) : IRequest<Result<IntegrationConfigDto>>;
public record GetIntegrationByKeyQuery(Guid TenantId, string ConfigKey) : IRequest<Result<IntegrationConfigDto>>;
public record UpdateIntegrationCommand(Guid Id, string Name, string BaseUrl,
    string? Description, string AuthType, string? AuthParams, string? DefaultHeaders,
    int TimeoutMs, string? RetryConfig, string? CircuitBreaker, string? Tags,
    string? Metadata, List<OperationDto> Operations, string UpdatedBy)
    : IRequest<Result<IntegrationConfigDto>>;
public record DeleteIntegrationCommand(Guid Id, string DeletedBy) : IRequest<Result>;
public record ChangeIntegrationStatusCommand(Guid Id, string Status, string UpdatedBy)
    : IRequest<Result<IntegrationConfigDto>>;
public record TestIntegrationCommand(Guid Id, string Operation, Dictionary<string, object> TestData)
    : IRequest<Result<object>>;
public record GetConfigHistoryQuery(Guid ConfigId) : IRequest<object>;
public record GetAuditLogQuery(Guid EntityId) : IRequest<object>;
