using BuildingBlocks.Abstractions.Connectors;
using ConfigService.Api.Interfaces;
using ConfigService.Api.Models;

namespace ConfigService.Api.Services;

/// <summary>
/// Service layer xử lý business logic cho config management.
/// - Versioning: mỗi update tạo version mới
/// - Governance: Draft → Review → Approved → Active
/// - Audit log: ghi lại mọi thay đổi
/// - Schema validation: validate config trước khi lưu
/// </summary>
public class ConfigManagementService : IConfigService
{
    private readonly IConfigRepository _repository;
    private readonly IConfigAuditRepository _auditRepository;
    private readonly ILogger<ConfigManagementService> _logger;

    public ConfigManagementService(
        IConfigRepository repository,
        IConfigAuditRepository auditRepository,
        ILogger<ConfigManagementService> logger)
    {
        _repository = repository;
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public Task<ConnectorConfig?> GetActiveAsync(string id) => _repository.GetActiveAsync(id);
    public Task<ConnectorConfig?> GetByVersionAsync(string id, int version) => _repository.GetByVersionAsync(id, version);
    public Task<List<ConnectorConfig>> GetAllVersionsAsync(string id) => _repository.GetAllVersionsAsync(id);
    public Task<List<ConnectorConfig>> GetAllAsync(string? status, string? connectorType) => _repository.GetAllAsync(status, connectorType);

    public async Task<ConnectorConfig> CreateAsync(ConnectorConfig config, string performedBy)
    {
        // Validate schema
        var errors = ValidateConfigSchema(config);
        if (errors.Count > 0)
            throw new InvalidOperationException($"Config validation failed: {string.Join("; ", errors)}");

        config.Version = 1;
        config.Status = ConfigStatus.Draft; // Bắt đầu từ Draft, không Active ngay
        config.CreatedAt = DateTime.UtcNow;
        config.UpdatedAt = DateTime.UtcNow;

        await _repository.CreateAsync(config);

        await _auditRepository.AddAsync(new ConfigAuditEntry
        {
            ConfigId = config.Id,
            Version = config.Version,
            Action = "Created",
            PerformedBy = performedBy,
            Details = $"Config '{config.Name}' created as Draft"
        });

        _logger.LogInformation("Config created: {Id} v{Version} by {User}", config.Id, config.Version, performedBy);
        return config;
    }

    public async Task<ConnectorConfig> UpdateAsync(string id, ConnectorConfig config, string performedBy)
    {
        var existing = await _repository.GetActiveAsync(id)
            ?? await GetLatestVersion(id)
            ?? throw new KeyNotFoundException($"Config '{id}' not found");

        // Validate
        var errors = ValidateConfigSchema(config);
        if (errors.Count > 0)
            throw new InvalidOperationException($"Config validation failed: {string.Join("; ", errors)}");

        // Deactivate old
        existing.Status = ConfigStatus.Inactive;
        await _repository.UpdateAsync(existing);

        // Create new version as Draft
        config.Id = id;
        config.Version = existing.Version + 1;
        config.Status = ConfigStatus.Draft;
        config.CreatedAt = existing.CreatedAt;
        config.UpdatedAt = DateTime.UtcNow;
        await _repository.CreateAsync(config);

        await _auditRepository.AddAsync(new ConfigAuditEntry
        {
            ConfigId = id,
            Version = config.Version,
            Action = "Updated",
            PerformedBy = performedBy,
            Details = $"v{existing.Version} → v{config.Version}"
        });

        _logger.LogInformation("Config updated: {Id} v{Old} → v{New} by {User}",
            id, existing.Version, config.Version, performedBy);
        return config;
    }

    /// <summary>
    /// Transition config status theo governance flow:
    /// Draft → Review → Approved → Active
    /// </summary>
    public async Task<ConnectorConfig> TransitionStatusAsync(string id, string newStatus, string performedBy)
    {
        var config = await GetLatestVersion(id)
            ?? throw new KeyNotFoundException($"Config '{id}' not found");

        if (!ConfigStatus.IsValidTransition(config.Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Invalid status transition: {config.Status} → {newStatus}");
        }

        var oldStatus = config.Status;
        config.Status = newStatus;
        config.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(config);

        await _auditRepository.AddAsync(new ConfigAuditEntry
        {
            ConfigId = id,
            Version = config.Version,
            Action = $"Status:{oldStatus}→{newStatus}",
            PerformedBy = performedBy
        });

        _logger.LogInformation("Config status changed: {Id} {Old} → {New} by {User}",
            id, oldStatus, newStatus, performedBy);
        return config;
    }

    public async Task<ConnectorConfig?> RollbackAsync(string id, int version, string performedBy)
    {
        var target = await _repository.GetByVersionAsync(id, version);
        if (target == null) return null;

        // Deactivate current
        var current = await _repository.GetActiveAsync(id);
        if (current != null)
        {
            current.Status = ConfigStatus.Inactive;
            await _repository.UpdateAsync(current);
        }

        target.Status = ConfigStatus.Active;
        target.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(target);

        await _auditRepository.AddAsync(new ConfigAuditEntry
        {
            ConfigId = id,
            Version = version,
            Action = "Rolledback",
            PerformedBy = performedBy,
            Details = $"Rolled back to v{version}"
        });

        _logger.LogInformation("Config rolled back: {Id} → v{Version} by {User}", id, version, performedBy);
        return target;
    }

    public async Task DeleteAsync(string id, string performedBy)
    {
        await _repository.SoftDeleteAsync(id);

        await _auditRepository.AddAsync(new ConfigAuditEntry
        {
            ConfigId = id,
            Action = "Deleted",
            PerformedBy = performedBy
        });

        _logger.LogInformation("Config deleted: {Id} by {User}", id, performedBy);
    }

    public Task<List<ConfigAuditEntry>> GetAuditLogAsync(string configId)
        => _auditRepository.GetByConfigIdAsync(configId);

    /// <summary>
    /// Validate config schema trước khi lưu.
    /// Đảm bảo config hợp lệ, tránh lỗi runtime.
    /// </summary>
    public List<string> ValidateConfigSchema(ConnectorConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Id))
            errors.Add("Config Id is required");

        if (string.IsNullOrWhiteSpace(config.Name))
            errors.Add("Config Name is required");

        if (string.IsNullOrWhiteSpace(config.ConnectorType))
            errors.Add("ConnectorType is required");

        if (string.IsNullOrWhiteSpace(config.Endpoint?.BaseUrl))
            errors.Add("Endpoint.BaseUrl is required");

        if (config.TimeoutMs <= 0)
            errors.Add("TimeoutMs must be positive");

        if (config.Operations == null || config.Operations.Count == 0)
            errors.Add("At least one Operation is required");

        foreach (var op in config.Operations ?? new())
        {
            if (string.IsNullOrWhiteSpace(op.Name))
                errors.Add("Operation.Name is required");
            if (string.IsNullOrWhiteSpace(op.Path))
                errors.Add($"Operation '{op.Name}': Path is required");
            if (string.IsNullOrWhiteSpace(op.HttpMethod))
                errors.Add($"Operation '{op.Name}': HttpMethod is required");
        }

        if (config.Retry != null)
        {
            if (config.Retry.MaxRetries < 0)
                errors.Add("Retry.MaxRetries cannot be negative");
            if (config.Retry.InitialDelayMs <= 0)
                errors.Add("Retry.InitialDelayMs must be positive");
        }

        return errors;
    }

    private async Task<ConnectorConfig?> GetLatestVersion(string id)
    {
        var versions = await _repository.GetAllVersionsAsync(id);
        return versions.OrderByDescending(v => v.Version).FirstOrDefault();
    }
}
