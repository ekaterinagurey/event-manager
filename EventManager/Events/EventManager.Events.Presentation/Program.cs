using EventManager.Bookings.Infrastructure.Cache;
using EventManager.Events.Application;
using EventManager.Events.Application.Interfaces;
using EventManager.Events.Infrastructure;
using EventManager.Events.Presentation.Extensions;
using EventManager.Events.Presentation.Middleware;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Подключение слоев
builder.Services.AddApplication();

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis:ConnectionString configuration is missing.");

var options = new ConfigurationOptions
{
    EndPoints = { redisConnectionString },
    Password = "secret",
    ConnectTimeout = 5000,
    SyncTimeout = 3000,
    AbortOnConnectFail = false,
};

var connection = await ConnectionMultiplexer.ConnectAsync(options);

builder.Services.AddSingleton<IConnectionMultiplexer>(connection);

builder.Services.AddSingleton<ICacheService, CacheService>();

builder.Services.AddInfrastructure(builder.Configuration);

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
