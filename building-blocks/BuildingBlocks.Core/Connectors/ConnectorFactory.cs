using System.Collections.Concurrent;
using System.Reflection;
using BuildingBlocks.Abstractions.Connectors;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Core.Connectors;

/// <summary>
/// Factory tạo connector instance dựa trên config.
/// Hỗ trợ 2 loại:
/// 1. Generic HTTP Connector (built-in, config-driven)
/// 2. Custom DLL Connector (plugin, load runtime)
/// </summary>
public interface IConnectorFactory
{
    IConnector Create(ConnectorConfig config);
    void RegisterConnector(string connectorType, Func<IConnector> factory);
    IReadOnlyCollection<string> GetRegisteredTypes();
}

public class ConnectorFactory : IConnectorFactory
{
    private readonly ILogger<ConnectorFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConcurrentDictionary<string, Func<IConnector>> _registry = new();
    private readonly ConcurrentDictionary<string, PluginLoadContext> _loadContexts = new();
    private readonly string _pluginDirectory;

    public ConnectorFactory(
        ILogger<ConnectorFactory> logger,
        IHttpClientFactory httpClientFactory,
        string? pluginDirectory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _pluginDirectory = pluginDirectory ?? Path.Combine(AppContext.BaseDirectory, "plugins");

        // Đăng ký built-in connector
        RegisterConnector("generic-http", () => new GenericHttpConnector(_httpClientFactory, _logger));

        // Scan và load tất cả plugin DLL
        LoadPlugins();
    }

    public IConnector Create(ConnectorConfig config)
    {
        if (_registry.TryGetValue(config.ConnectorType, out var factory))
        {
            _logger.LogInformation(
                "Creating connector: {ConnectorType} for config: {ConfigId}",
                config.ConnectorType, config.Id);

            try
            {
                return factory();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to instantiate connector: {Type}", config.ConnectorType);
                throw new InvalidOperationException(
                    $"Connector '{config.ConnectorType}' failed to instantiate: {ex.Message}", ex);
            }
        }

        // Thử load DLL connector nếu chưa có trong registry
        var connector = TryLoadDllConnector(config.ConnectorType);
        if (connector != null) return connector;

        throw new InvalidOperationException(
            $"Connector type '{config.ConnectorType}' not found. " +
            $"Available types: {string.Join(", ", _registry.Keys)}");
    }

    public void RegisterConnector(string connectorType, Func<IConnector> factory)
    {
        _registry.AddOrUpdate(connectorType, factory, (_, _) => factory);
        _logger.LogInformation("Registered connector type: {ConnectorType}", connectorType);
    }

    public IReadOnlyCollection<string> GetRegisteredTypes() => _registry.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Scan thư mục plugins và load tất cả DLL chứa IConnector implementation.
    /// Mỗi DLL được load trong isolated AssemblyLoadContext riêng.
    /// Nếu 1 DLL lỗi → log error, skip, KHÔNG crash toàn hệ thống.
    /// </summary>
    private void LoadPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            _logger.LogWarning("Plugin directory not found: {Dir}. Creating it.", _pluginDirectory);
            Directory.CreateDirectory(_pluginDirectory);
            return;
        }

        foreach (var dllPath in Directory.GetFiles(_pluginDirectory, "*.dll"))
        {
            try
            {
                LoadConnectorFromDll(dllPath);
            }
            catch (Exception ex)
            {
                // ❗ FIX #1: 1 plugin lỗi KHÔNG crash toàn hệ thống
                _logger.LogError(ex,
                    "Failed to load plugin: {DllPath}. Skipping. Error: {Error}",
                    dllPath, ex.Message);
            }
        }
    }

    /// <summary>
    /// Load DLL trong isolated AssemblyLoadContext (collectible = true).
    /// - Mỗi plugin có context riêng → tránh version conflict
    /// - collectible = true → có thể unload khi cần
    /// - Error boundary: exception trong plugin không lan ra host
    /// </summary>
    private void LoadConnectorFromDll(string dllPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(dllPath);
        if (_loadContexts.ContainsKey(fileName))
        {
            _logger.LogDebug("Plugin already loaded: {FileName}", fileName);
            return;
        }

        _logger.LogInformation("Loading plugin DLL: {DllPath}", dllPath);

        var loadContext = new PluginLoadContext(dllPath);
        var assembly = loadContext.LoadFromAssemblyPath(Path.GetFullPath(dllPath));

        var connectorTypes = assembly.GetTypes()
            .Where(t => typeof(IConnector).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        if (connectorTypes.Count == 0)
        {
            _logger.LogWarning("No IConnector implementations found in: {Dll}", fileName);
            loadContext.Unload(); // Unload nếu không có connector
            return;
        }

        _loadContexts.TryAdd(fileName, loadContext);

        foreach (var type in connectorTypes)
        {
            try
            {
                var instance = (IConnector)Activator.CreateInstance(type)!;
                var connectorType = instance.ConnectorType;

                // Wrap factory trong error boundary
                RegisterConnector(connectorType, () =>
                {
                    try
                    {
                        return (IConnector)Activator.CreateInstance(type)!;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Plugin connector '{connectorType}' from '{fileName}' failed: {ex.Message}", ex);
                    }
                });

                _logger.LogInformation(
                    "Loaded plugin connector: {Type} from {Dll}", connectorType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to register connector type from {Dll}/{Type}",
                    fileName, type.FullName);
            }
        }
    }

    /// <summary>
    /// Unload một plugin DLL (giải phóng memory).
    /// Chỉ hoạt động vì AssemblyLoadContext là collectible.
    /// </summary>
    public bool UnloadPlugin(string pluginName)
    {
        if (!_loadContexts.TryRemove(pluginName, out var context))
            return false;

        // Remove registered connectors từ plugin này
        // (simplified — production cần track connector→plugin mapping)
        context.Unload();
        _logger.LogInformation("Plugin unloaded: {Plugin}", pluginName);
        return true;
    }

    private IConnector? TryLoadDllConnector(string connectorType)
    {
        var dllName = $"Connector.{connectorType}.dll";
        var dllPath = Path.Combine(_pluginDirectory, dllName);

        if (!File.Exists(dllPath)) return null;

        LoadConnectorFromDll(dllPath);
        return _registry.TryGetValue(connectorType, out var factory) ? factory() : null;
    }
}

/// <summary>
/// Isolated AssemblyLoadContext cho mỗi plugin DLL.
/// - collectible: true → có thể unload (giải phóng memory)
/// - Mỗi plugin có dependency resolution riêng → tránh conflict
/// </summary>
public class PluginLoadContext : System.Runtime.Loader.AssemblyLoadContext
{
    private readonly System.Runtime.Loader.AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(
        name: Path.GetFileNameWithoutExtension(pluginPath),
        isCollectible: true)
    {
        _resolver = new System.Runtime.Loader.AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Không load shared assemblies (BuildingBlocks.Abstractions) từ plugin
        // → dùng chung với host, tránh type mismatch
        if (assemblyName.Name?.StartsWith("BuildingBlocks.") == true)
            return null; // Fallback to default context

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }
}
