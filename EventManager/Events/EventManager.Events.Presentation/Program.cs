using EventManager.Events.Application;
using EventManager.Events.Application.Options;
using EventManager.Events.Infrastructure;
using EventManager.Events.Presentation.Extensions;
using EventManager.Events.Presentation.Middleware;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var redisConfig = builder.Configuration
    .GetSection(RedisCacheOptions.SectionName)
    .Get<RedisCacheOptions>()
    ?? throw new InvalidOperationException("Секция конфигурации 'Redis' отсутствует в appsettings.json.");

builder.Services.Configure<RedisCacheOptions>(builder.Configuration.GetSection(RedisCacheOptions.SectionName));

// 2. Формируем опции подключения на основе конфигурации
var options = ConfigurationOptions.Parse(redisConfig.ConnectionString);

if (!string.IsNullOrWhiteSpace(redisConfig.Password))
{
    options.Password = builder.Configuration["REDIS_SECRET"] ?? throw new InvalidOperationException("Пароль для Redis не задан");
}

options.ConnectTimeout = redisConfig.ConnectTimeoutMs;
options.SyncTimeout = redisConfig.SyncTimeoutMs;
options.AbortOnConnectFail = false;
options.ConnectRetry = 3;

// 3. Асинхронное подключение
var connection = await ConnectionMultiplexer.ConnectAsync(options);

// Подключение слоев
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, connection);

builder.Services.AddControllers();
builder.Services.AddConfiguredSwagger();

const string serviceName = "events-service";

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
