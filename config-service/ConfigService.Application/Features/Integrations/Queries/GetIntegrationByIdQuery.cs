using ConfigService.Application.Common.Models;
using ConfigService.Domain.Repositories;
using MediatR;

namespace ConfigService.Application.Features.Integrations.Queries;

public record GetIntegrationByIdQuery(Guid Id) : IRequest<Result<IntegrationConfigDto>>;

public class GetIntegrationByIdQueryHandler
    : IRequestHandler<GetIntegrationByIdQuery, Result<IntegrationConfigDto>>
{
    private readonly IIntegrationConfigRepository _repository;
    public GetIntegrationByIdQueryHandler(IIntegrationConfigRepository r) => _repository = r;

    public async Task<Result<IntegrationConfigDto>> Handle(GetIntegrationByIdQuery q, CancellationToken ct)
    {
        var config = await _repository.GetByIdAsync(q.Id, ct);
        return config == null
            ? Result<IntegrationConfigDto>.Failure($"Config '{q.Id}' not found", "NOT_FOUND")
            : Result<IntegrationConfigDto>.Success(IntegrationConfigDto.FromEntity(config));
    }
}

public record GetIntegrationByKeyQuery(string TenantId, string ConfigKey) : IRequest<Result<IntegrationConfigDto>>;

public class GetIntegrationByKeyQueryHandler
    : IRequestHandler<GetIntegrationByKeyQuery, Result<IntegrationConfigDto>>
{
    private readonly IIntegrationConfigRepository _repository;
    public GetIntegrationByKeyQueryHandler(IIntegrationConfigRepository r) => _repository = r;

    public async Task<Result<IntegrationConfigDto>> Handle(GetIntegrationByKeyQuery q, CancellationToken ct)
    {
        var config = await _repository.GetByKeyAsync(q.TenantId, q.ConfigKey, ct);
        return config == null
            ? Result<IntegrationConfigDto>.Failure($"Config '{q.ConfigKey}' not found", "NOT_FOUND")
            : Result<IntegrationConfigDto>.Success(IntegrationConfigDto.FromEntity(config));
    }
}
