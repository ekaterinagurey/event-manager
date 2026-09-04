using EventManager.Application.Interfaces.Authentication;
using EventManager.Application.Interfaces.Repositories;
using EventManager.Infrastructure.Authentication;
using EventManager.Infrastructure.DataAccess;
using EventManager.Infrastructure.Repositories;
using EventManager.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

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

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IEventRepository, EventRepository>();
            services.AddScoped<IBookingRepository, BookingRepository>();

            // 6. Аутентификация
            var jwtSection = configuration.GetSection("Jwt");

            // Получаем секрет из secrets
            var secret = configuration["JWT_SECRET"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("JWT Secret не задан");
            }

            // Регистрация IOptions<JwtOptions> в DI
            services.Configure<JwtOptions>(options =>
            {
                jwtSection.Bind(options);
                options.Secret = secret!;
            });
           
            // Регистрация аутентификации 
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSection["Issuer"],

                        ValidateAudience = true,
                        ValidAudience = jwtSection["Audience"],

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                    };
                });

            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

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
