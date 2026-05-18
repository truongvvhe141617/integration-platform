using ConfigService.Application;
using ConfigService.Infrastructure;
using ConfigService.Api.Repositories;
using ConfigService.Api.Interfaces;
using ConfigService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Config Service API", Version = "v1" });
});

// ── Pattern iolis-instrument: tách DI ra 2 extension methods ──
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

// ── Legacy InMemory repositories (backward compatible) ──
builder.Services.AddSingleton<IConfigRepository, InMemoryConfigRepository>();
builder.Services.AddSingleton<IConfigAuditRepository, InMemoryConfigAuditRepository>();
builder.Services.AddScoped<IConfigService, ConfigManagementService>();

// ── CORS ──
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins?.Length > 0)
            policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Auto-migrate + seed data
    await ConfigService.Api.SeedData.SeedAsync(app.Services);
}

app.UseCors();

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
