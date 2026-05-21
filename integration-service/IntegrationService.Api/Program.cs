using BuildingBlocks.Abstractions.Connectors;
using BuildingBlocks.Core.Connectors;
using BuildingBlocks.Core.Localization;
using BuildingBlocks.Core.Mapping;
using BuildingBlocks.Core.Messaging;
using IntegrationService.Api.Middleware;
using IntegrationService.Application;
using IntegrationService.Infrastructure;
using IntegrationService.Infrastructure.Messaging;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Integration Service API", Version = "v1" });
});

// ── Clean Architecture: Application + Infrastructure layers ──
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── HTTP Clients ──
builder.Services.AddHttpClient("ConfigService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ConfigService"] ?? "http://localhost:5002");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");
});
builder.Services.AddHttpClient("integration-connector", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Building Blocks (shared infrastructure) ──
builder.Services.AddSingleton<IConnectorFactory>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ConnectorFactory>>();
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var pluginDir = builder.Configuration["Connectors:PluginDirectory"]
        ?? Path.Combine(AppContext.BaseDirectory, "plugins");
    return new ConnectorFactory(logger, httpFactory, pluginDir);
});
builder.Services.AddSingleton<IMappingEngine, MappingEngine>();
builder.Services.AddSingleton<IMessageLocalizer>(sp =>
    new MessageLocalizer(sp.GetRequiredService<ILogger<MessageLocalizer>>()));

// ── RabbitMQ (optional) ──
var rabbitHost = builder.Configuration["RabbitMQ:Host"];
if (!string.IsNullOrEmpty(rabbitHost))
{
    builder.Services.AddSingleton<IConnection>(sp =>
    {
        var factory = new ConnectionFactory
        {
            HostName = rabbitHost,
            Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = builder.Configuration["RabbitMQ:Username"] ?? "admin",
            Password = builder.Configuration["RabbitMQ:Password"] ?? "admin",
            VirtualHost = builder.Configuration["RabbitMQ:VirtualHost"] ?? "/"
        };
        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
    });
    builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
    builder.Services.AddHostedService<IntegrationMessageConsumer>();
}
else
{
    builder.Services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();
}

// ── Health Checks ──
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();
