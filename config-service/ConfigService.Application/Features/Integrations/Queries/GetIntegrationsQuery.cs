using ConfigService.Application.Common.Models;
using ConfigService.Domain.Repositories;
using MediatR;

namespace ConfigService.Application.Features.Integrations.Queries;

public record GetIntegrationsQuery(
    string TenantId, string? Search, string? Status,
    string? Tags, int Page, int PageSize
) : IRequest<PagedResult<IntegrationConfigDto>>;

public class GetIntegrationsQueryHandler
    : IRequestHandler<GetIntegrationsQuery, PagedResult<IntegrationConfigDto>>
{
    private readonly IIntegrationConfigRepository _repository;

    public GetIntegrationsQueryHandler(IIntegrationConfigRepository repository)
        => _repository = repository;

    public async Task<PagedResult<IntegrationConfigDto>> Handle(
        GetIntegrationsQuery query, CancellationToken ct)
    {
        var (items, total) = await _repository.GetPagedAsync(
            query.TenantId, query.Search, query.Status,
            query.Tags, query.Page, query.PageSize, ct);

        return new PagedResult<IntegrationConfigDto>
        {
            Items = items.Select(IntegrationConfigDto.FromEntity).ToList(),
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
