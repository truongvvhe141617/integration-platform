using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WebSocketService.Api.Hubs;

/// <summary>
/// Quản lý WebSocket connections.
/// Client kết nối → đăng ký correlationId → nhận kết quả real-time.
/// 
/// Flow:
/// 1. Client mở WebSocket: ws://localhost:5004/ws?clientId=app-a
/// 2. Client gửi async integration request (qua HTTP hoặc WebSocket)
/// 3. Integration Service xử lý → publish result lên RabbitMQ
/// 4. WebSocket Service consume result → push tới client qua WebSocket
/// </summary>
public class WebSocketConnectionManager
{
    // clientId → WebSocket connection
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    // correlationId → clientId (để route result về đúng client)
    private readonly ConcurrentDictionary<string, string> _subscriptions = new();
    private readonly ILogger<WebSocketConnectionManager> _logger;

    public WebSocketConnectionManager(ILogger<WebSocketConnectionManager> logger)
    {
        _logger = logger;
    }

    public void AddConnection(string clientId, WebSocket socket)
    {
        _connections.AddOrUpdate(clientId, socket, (_, _) => socket);
        _logger.LogInformation("WebSocket connected: {ClientId}. Total: {Count}", clientId, _connections.Count);
    }

    public void RemoveConnection(string clientId)
    {
        _connections.TryRemove(clientId, out _);
        // Remove all subscriptions for this client
        foreach (var sub in _subscriptions.Where(s => s.Value == clientId))
            _subscriptions.TryRemove(sub.Key, out _);
        _logger.LogInformation("WebSocket disconnected: {ClientId}. Total: {Count}", clientId, _connections.Count);
    }

    public void Subscribe(string correlationId, string clientId)
    {
        _subscriptions.TryAdd(correlationId, clientId);
        _logger.LogDebug("Subscribed: {CorrId} → {ClientId}", correlationId, clientId);
    }

    /// <summary>Gửi message tới client theo correlationId</summary>
    public async Task SendToCorrelationAsync(string correlationId, object message)
    {
        if (!_subscriptions.TryGetValue(correlationId, out var clientId)) return;
        if (!_connections.TryGetValue(clientId, out var socket)) return;
        if (socket.State != WebSocketState.Open) return;

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);

        _logger.LogInformation("Sent to {ClientId} via WebSocket: CorrId={CorrId}", clientId, correlationId);
        _subscriptions.TryRemove(correlationId, out _); // One-time delivery
    }

    /// <summary>Broadcast tới tất cả clients</summary>
    public async Task BroadcastAsync(object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var (clientId, socket) in _connections)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    public int ConnectionCount => _connections.Count;
}
