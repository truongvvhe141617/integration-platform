using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using WebSocketService.Api.Consumers;
using WebSocketService.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── WebSocket Manager ──
builder.Services.AddSingleton<WebSocketConnectionManager>();

// ── RabbitMQ ──
var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory
    {
        HostName = rabbitHost,
        Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
        UserName = builder.Configuration["RabbitMQ:Username"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddHostedService<ResultConsumer>();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ── WebSocket middleware ──
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(30) });

// WebSocket endpoint: ws://localhost:5004/ws?clientId=xxx
app.Map("/ws", async (HttpContext context, WebSocketConnectionManager wsManager, ILogger<Program> logger) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("WebSocket connection required. Use ws://localhost:5004/ws?clientId=xxx");
        return;
    }

    var clientId = context.Request.Query["clientId"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    using var socket = await context.WebSockets.AcceptWebSocketAsync();

    wsManager.AddConnection(clientId, socket);
    logger.LogInformation("Client connected: {ClientId}", clientId);

    // Send welcome message
    var welcome = JsonSerializer.Serialize(new { type = "connected", clientId, message = "WebSocket connected. Send subscribe messages to receive results." });
    await socket.SendAsync(Encoding.UTF8.GetBytes(welcome), WebSocketMessageType.Text, true, CancellationToken.None);

    // Listen for messages from client
    var buffer = new byte[4096];
    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var json = JsonSerializer.Deserialize<JsonElement>(message);

                // Client gửi: { "type": "subscribe", "correlationId": "xxx" }
                if (json.TryGetProperty("type", out var type) && type.GetString() == "subscribe"
                    && json.TryGetProperty("correlationId", out var corrId))
                {
                    wsManager.Subscribe(corrId.GetString()!, clientId);
                    var ack = JsonSerializer.Serialize(new { type = "subscribed", correlationId = corrId.GetString() });
                    await socket.SendAsync(Encoding.UTF8.GetBytes(ack), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
    catch (WebSocketException) { }
    finally
    {
        wsManager.RemoveConnection(clientId);
        logger.LogInformation("Client disconnected: {ClientId}", clientId);
    }
});

// Status endpoint
app.MapGet("/status", (WebSocketConnectionManager wsManager) => new
{
    service = "WebSocket Service",
    connections = wsManager.ConnectionCount
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapHealthChecks("/health");

app.Run();
