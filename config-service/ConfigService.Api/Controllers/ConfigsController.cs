using BuildingBlocks.Abstractions.Connectors;
using ConfigService.Api.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.Api.Controllers;

/// <summary>
/// Thin controller — delegate mọi logic sang IConfigService.
/// Chỉ xử lý HTTP concerns: routing, status codes, headers.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class ConfigsController : ControllerBase
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigsController> _logger;

    public ConfigsController(IConfigService configService, ILogger<ConfigsController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var config = await _configService.GetActiveAsync(id);
        return config != null ? Ok(config) : NotFound();
    }

    [HttpGet("{id}/versions/{version:int}")]
    public async Task<IActionResult> GetByVersion(string id, int version)
    {
        var config = await _configService.GetByVersionAsync(id, version);
        return config != null ? Ok(config) : NotFound();
    }

    [HttpGet("{id}/versions")]
    public async Task<IActionResult> GetVersions(string id)
    {
        var versions = await _configService.GetAllVersionsAsync(id);
        return Ok(versions);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] string? connectorType = null)
    {
        var configs = await _configService.GetAllAsync(status, connectorType);
        return Ok(configs);
    }

    /// <summary>Tạo config mới (status = Draft)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ConnectorConfig config)
    {
        var performedBy = GetCurrentUser();
        try
        {
            var created = await _configService.CreateAsync(config, performedBy);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Cập nhật config → tạo version mới (status = Draft)</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ConnectorConfig config)
    {
        var performedBy = GetCurrentUser();
        try
        {
            var updated = await _configService.UpdateAsync(id, config, performedBy);
            return Ok(updated);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>
    /// Chuyển status theo governance flow: Draft → Review → Approved → Active
    /// POST /api/v1/configs/{id}/transition?status=Review
    /// </summary>
    [HttpPost("{id}/transition")]
    public async Task<IActionResult> TransitionStatus(
        string id, [FromQuery] string status)
    {
        var performedBy = GetCurrentUser();
        try
        {
            var config = await _configService.TransitionStatusAsync(id, status, performedBy);
            return Ok(config);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Rollback về version cụ thể</summary>
    [HttpPost("{id}/rollback/{version:int}")]
    public async Task<IActionResult> Rollback(string id, int version)
    {
        var performedBy = GetCurrentUser();
        var config = await _configService.RollbackAsync(id, version, performedBy);
        return config != null ? Ok(config) : NotFound(new { error = $"Version {version} not found" });
    }

    /// <summary>Lấy audit log của config</summary>
    [HttpGet("{id}/audit")]
    public async Task<IActionResult> GetAuditLog(string id)
    {
        var entries = await _configService.GetAuditLogAsync(id);
        return Ok(entries);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var performedBy = GetCurrentUser();
        await _configService.DeleteAsync(id, performedBy);
        return NoContent();
    }

    /// <summary>Lấy user từ JWT claims (hoặc header fallback cho dev)</summary>
    private string GetCurrentUser()
    {
        return User.Identity?.Name
            ?? HttpContext.Request.Headers["X-User-Id"].FirstOrDefault()
            ?? "system";
    }
}
