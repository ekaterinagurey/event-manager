using EventManager.Application.Repositories.Interfaces;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using EventManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EventManager.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
                                                           IConfiguration configuration)
        {
            //Запонение пароля в connectionString
            // 1. Считываем базовую строку подключения из appsettings.json
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // 2. Считываем пароль: Configuration проверяет user-secrets и Environment Variables
            var postgresPassword = configuration["POSTGRES_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

            // 3. Подставляем пароль через NpgsqlConnectionStringBuilder
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);

            if (!string.IsNullOrWhiteSpace(postgresPassword))
            {
                connectionStringBuilder.Password = postgresPassword;
            }

            // 5. Регистрируем DbContext с готовой строкой
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionStringBuilder.ConnectionString,
                                  npgsqlOptions =>
                                  {
                                      npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                                  }
                ));

            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            return services;
        }

        public static async Task ApplyMigrationsAsync(this IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
            }
        }
    }
}
