using BusinessService.Api.Interfaces;
using BusinessService.Api.Services;
using BusinessService.Application;
using BusinessService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Pattern iolis-instrument ──
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── HTTP Client cho Integration Service ──
builder.Services.AddHttpClient("IntegrationService", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:IntegrationService"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Application Services ──
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    if (!context.Request.Headers.ContainsKey("X-Correlation-Id"))
        context.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-Id"] = context.Request.Headers["X-Correlation-Id"].ToString();
    await next();
});

app.UseRouting();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
