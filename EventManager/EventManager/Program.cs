using EventManager.Application.BackgroundServices;
using EventManager.Infrastructure.DataAccess;
using EventManager.Middleware;
using EventManager.Repositories;
using EventManager.Application.Repositories.Interfaces;
using EventManager.Application.Services;
using EventManager.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using EventManager.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

//Запонение пароля в connectionString
// 1. Считываем базовую строку подключения из appsettings.json
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. Считываем пароль: Configuration проверяет user-secrets и Environment Variables
var postgresPassword = builder.Configuration["POSTGRES_PASSWORD"]
    ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

// 3. Подставляем пароль через NpgsqlConnectionStringBuilder
var connectionStringBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);

if (!string.IsNullOrWhiteSpace(postgresPassword))
{
    connectionStringBuilder.Password = postgresPassword;
}

// 5. Регистрируем DbContext с готовой строкой
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionStringBuilder.ConnectionString));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionStringBuilder.ConnectionString,
                      npgsqlOptions =>
                            {
                                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                            }
    ));

builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddHostedService<BookingProcessingService>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = string.Join("; ", errors)
        };

        return new BadRequestObjectResult(problemDetails);
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
