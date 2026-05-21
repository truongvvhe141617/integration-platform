using ConfigService.Application.Common.Models;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using MediatR;

namespace ConfigService.Application.Features.Integrations.Commands;

// ── Update ──
public record UpdateIntegrationCommand(
    Guid Id, string Name, string BaseUrl, string? Description,
    string AuthType, string? AuthParams, string? DefaultHeaders,
    int TimeoutMs, string? RetryConfig, string? CircuitBreaker,
    string? Tags, string? Metadata, List<OperationDto> Operations, string UpdatedBy
) : IRequest<Result<IntegrationConfigDto>>;

public class UpdateIntegrationCommandHandler : IRequestHandler<UpdateIntegrationCommand, Result<IntegrationConfigDto>>
{
    private readonly IIntegrationConfigRepository _repo;
    public UpdateIntegrationCommandHandler(IIntegrationConfigRepository repo) => _repo = repo;

    public async Task<Result<IntegrationConfigDto>> Handle(UpdateIntegrationCommand cmd, CancellationToken ct)
    {
        var config = await _repo.GetByIdAsync(cmd.Id, ct);
        if (config == null) return Result<IntegrationConfigDto>.Failure("Integration not found");

        // Update basic fields only (operations handled separately in repository)
        config.Update(cmd.Name, cmd.BaseUrl, cmd.Description, cmd.AuthType, cmd.AuthParams,
            cmd.DefaultHeaders, cmd.TimeoutMs, cmd.RetryConfig, cmd.CircuitBreaker,
            cmd.Tags, cmd.Metadata, cmd.UpdatedBy);

        // Build new operations list
        var newOperations = new List<IntegrationOperation>();
        foreach (var opDto in cmd.Operations)
        {
            var operation = IntegrationOperation.Create(
                config.Id, opDto.Name, opDto.HttpMethod, opDto.Path,
                opDto.ContentType, opDto.Description);

            foreach (var m in opDto.RequestMappings ?? new())
                operation.AddRequestMapping(m.SourceField, m.TargetField, m.DefaultValue, m.Transform, m.IsRequired, m.SortOrder);
            foreach (var m in opDto.ResponseMappings ?? new())
                operation.AddResponseMapping(m.SourceField, m.TargetField, m.DefaultValue, m.Transform, m.SortOrder);
            foreach (var v in opDto.Validations ?? new())
                operation.AddValidationRule(v.Field, v.Rule, v.ErrorMessage, v.SortOrder);

            newOperations.Add(operation);
        }

        await _repo.UpdateWithOperationsAsync(config, newOperations, ct);
        
        // Reload to get fresh data
        var updated = await _repo.GetByIdAsync(cmd.Id, ct);
        return Result<IntegrationConfigDto>.Success(IntegrationConfigDto.FromEntity(updated!));
    }
}

// ── Delete ──
public record DeleteIntegrationCommand(Guid Id, string DeletedBy) : IRequest<Result>;

public class DeleteIntegrationCommandHandler : IRequestHandler<DeleteIntegrationCommand, Result>
{
    private readonly IIntegrationConfigRepository _repo;
    public DeleteIntegrationCommandHandler(IIntegrationConfigRepository repo) => _repo = repo;

    public async Task<Result> Handle(DeleteIntegrationCommand cmd, CancellationToken ct)
    {
        var config = await _repo.GetByIdAsync(cmd.Id, ct);
        if (config == null) return Result.Failure("Integration not found");

        await _repo.DeleteAsync(config, ct);
        return Result.Success();
    }
}

// ── Change Status ──
public record ChangeIntegrationStatusCommand(Guid Id, string Status, string UpdatedBy)
    : IRequest<Result<IntegrationConfigDto>>;

public class ChangeStatusCommandHandler : IRequestHandler<ChangeIntegrationStatusCommand, Result<IntegrationConfigDto>>
{
    private readonly IIntegrationConfigRepository _repo;
    public ChangeStatusCommandHandler(IIntegrationConfigRepository repo) => _repo = repo;

    public async Task<Result<IntegrationConfigDto>> Handle(ChangeIntegrationStatusCommand cmd, CancellationToken ct)
    {
        var config = await _repo.GetByIdAsync(cmd.Id, ct);
        if (config == null) return Result<IntegrationConfigDto>.Failure("Integration not found");

        if (cmd.Status == "active") config.Activate(cmd.UpdatedBy);
        else if (cmd.Status == "inactive") config.Deactivate(cmd.UpdatedBy);
        else return Result<IntegrationConfigDto>.Failure($"Invalid status: {cmd.Status}");

        await _repo.UpdateAsync(config, ct);
        return Result<IntegrationConfigDto>.Success(IntegrationConfigDto.FromEntity(config));
    }
}

// ── Test (stub) ──
public record TestIntegrationCommand(Guid Id, string Operation, Dictionary<string, object> TestData)
    : IRequest<Result<object>>;

public class TestIntegrationCommandHandler : IRequestHandler<TestIntegrationCommand, Result<object>>
{
    public Task<Result<object>> Handle(TestIntegrationCommand cmd, CancellationToken ct)
    {
        // TODO: Implement actual test via Integration Service
        var result = new { message = "Test not implemented yet", configId = cmd.Id, operation = cmd.Operation };
        return Task.FromResult(Result<object>.Success(result));
    }
}

// ── History (stub) ──
public record GetConfigHistoryQuery(Guid ConfigId) : IRequest<object>;

public class GetConfigHistoryQueryHandler : IRequestHandler<GetConfigHistoryQuery, object>
{
    public Task<object> Handle(GetConfigHistoryQuery request, CancellationToken ct)
        => Task.FromResult<object>(new { items = Array.Empty<object>(), total = 0 });
}

// ── Audit (stub) ──
public record GetAuditLogQuery(Guid EntityId) : IRequest<object>;

public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, object>
{
    public Task<object> Handle(GetAuditLogQuery request, CancellationToken ct)
        => Task.FromResult<object>(new { items = Array.Empty<object>(), total = 0 });
}
