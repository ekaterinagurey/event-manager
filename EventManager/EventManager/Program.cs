using EventManager.BackgroundServices;
using EventManager.DataAccess;
using EventManager.Middleware;
using EventManager.Repositories;
using EventManager.Repositories.Interfaces;
using EventManager.Services;
using EventManager.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
