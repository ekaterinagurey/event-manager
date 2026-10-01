using EventManager.Bookings.Infrastructure.Cache;
using EventManager.Events.Application;
using EventManager.Events.Application.Interfaces;
using EventManager.Events.Application.Options;
using EventManager.Events.Infrastructure;
using EventManager.Events.Presentation.Extensions;
using EventManager.Events.Presentation.Middleware;
using Microsoft.Extensions.Options;
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
    options.Password = redisConfig.Password;
}

options.ConnectTimeout = redisConfig.ConnectTimeoutMs;
options.SyncTimeout = redisConfig.SyncTimeoutMs;
options.AbortOnConnectFail = false;
options.ConnectRetry = 3;

// 3. Асинхронное подключение
var connection = await ConnectionMultiplexer.ConnectAsync(options);

builder.Services.AddSingleton<ICacheService, CacheService>();

// Подключение слоев
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, connection);

builder.Services.AddControllers();
builder.Services.AddConfiguredSwagger();

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
app.MapControllers();

// Infrastructure - Migrations
await app.Services.ApplyMigrationsAsync();

app.Run();
