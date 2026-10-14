using EventManager.Bookings.Application;
using EventManager.Bookings.Infrastructure;
using EventManager.Bookings.Presentation.Extensions;
using EventManager.Bookings.Presentation.Middleware;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// 1. Читаем имя сервиса и OTLP Endpoint из конфигурации
var serviceName = builder.Configuration["ServiceName"] ?? "unknown-service";
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];

// Подключение Serilog как провайдера логирования
builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("ServiceName", serviceName) // для идентификации источника
       .WriteTo.Console(new CompactJsonFormatter());
});

// Подключение слоев
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddConfiguredSwagger();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddOtlpExporter(options =>
        {
            var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                options.Endpoint = new Uri(otlpEndpoint);
            }
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());


var app = builder.Build();

// Структурированный лог HTTP-запросов (ставится в начале конвейера middleware)
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Эндпоинт сбора метрик Prometheus (доступен по /metrics)
app.MapPrometheusScrapingEndpoint();

app.MapControllers();

// Infrastructure - Migrations
await app.Services.ApplyMigrationsAsync();

app.Run();
