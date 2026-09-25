using EventManager.Events.Application;
using EventManager.Events.Infrastructure;
using EventManager.Events.Presentation.Extensions;
using EventManager.Events.Presentation.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Подключение слоев
builder.Services.AddApplication();
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
