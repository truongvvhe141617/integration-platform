using ConfigService.Application.Common.Models;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using MediatR;

namespace ConfigService.Application.Features.Integrations.Commands;

// ── Command ──
public record CreateIntegrationCommand(
    string TenantId,
    string ConfigKey,
    string Name,
    string? Description,
    string ConnectorType,
    string BaseUrl,
    string? DefaultHeaders,
    string AuthType,
    string? AuthParams,
    int TimeoutMs,
    string? RetryConfig,
    string? CircuitBreaker,
    string? Tags,
    string? Metadata,
    List<OperationDto> Operations,
    string CreatedBy
) : IRequest<Result<IntegrationConfigDto>>;

// ── Handler ──
public class CreateIntegrationCommandHandler
    : IRequestHandler<CreateIntegrationCommand, Result<IntegrationConfigDto>>
{
    private readonly IIntegrationConfigRepository _repository;

    public CreateIntegrationCommandHandler(IIntegrationConfigRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IntegrationConfigDto>> Handle(
        CreateIntegrationCommand cmd, CancellationToken ct)
    {
        // Check duplicate
        if (await _repository.ExistsAsync(cmd.TenantId, cmd.ConfigKey, ct))
            return Result<IntegrationConfigDto>.Failure($"Config key '{cmd.ConfigKey}' already exists");

        // Create domain entity
        var config = IntegrationConfig.Create(
            cmd.TenantId, cmd.ConfigKey, cmd.Name,
            cmd.BaseUrl, cmd.ConnectorType, cmd.CreatedBy);

        config.Update(cmd.Name, cmd.BaseUrl, cmd.Description,
            cmd.AuthType, cmd.AuthParams, cmd.DefaultHeaders,
            cmd.TimeoutMs, cmd.RetryConfig, cmd.CircuitBreaker,
            cmd.Tags, cmd.Metadata, cmd.CreatedBy);

        // Add operations
        foreach (var opDto in cmd.Operations)
        {
            var op = IntegrationOperation.Create(
                config.Id, opDto.Name, opDto.HttpMethod, opDto.Path,
                opDto.ContentType, opDto.Description);

            foreach (var m in opDto.RequestMappings)
                op.AddRequestMapping(m.SourceField, m.TargetField, m.DefaultValue, m.Transform, m.IsRequired, m.SortOrder);

            foreach (var m in opDto.ResponseMappings)
                op.AddResponseMapping(m.SourceField, m.TargetField, m.DefaultValue, m.Transform, m.SortOrder);

            foreach (var v in opDto.Validations)
                op.AddValidationRule(v.Field, v.Rule, v.ErrorMessage, v.SortOrder);

            config.AddOperation(op);
        }

        await _repository.AddAsync(config, ct);
        return Result<IntegrationConfigDto>.Success(IntegrationConfigDto.FromEntity(config));
    }
}
