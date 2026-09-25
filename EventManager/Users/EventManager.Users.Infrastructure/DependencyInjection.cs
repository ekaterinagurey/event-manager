using EventManager.Users.Application.Interfaces;
using EventManager.Users.Domain.Repositories;
using EventManager.Users.Infrastructure.Authentication;
using EventManager.Users.Infrastructure.DataAccess;
using EventManager.Users.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

namespace EventManager.Users.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
                                                           IConfiguration configuration)
        {
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var postgresPassword = configuration["USER_DB_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("USER_DB_PASSWORD");

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);

            if (!string.IsNullOrWhiteSpace(postgresPassword))
            {
                connectionStringBuilder.Password = postgresPassword;
            }

            services.AddDbContext<UserDbContext>(options =>
                options.UseNpgsql(connectionStringBuilder.ConnectionString,
                                  npgsqlOptions =>
                                  {
                                      npgsqlOptions.MigrationsAssembly(typeof(UserDbContext).Assembly.FullName);
                                  }
                ));

            services.AddScoped<IUserRepository, UserRepository>();
           
            var jwtSection = configuration.GetSection("Jwt");

            // Получаем секрет из secrets
            var secret = configuration["JWT_SECRET"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new InvalidOperationException("JWT Secret не задан");
            }

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
                var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
                await db.Database.MigrateAsync();
            }
        }
    }
}
