using EventManager.Events.Application.Interfaces;
using EventManager.Events.Domain.Repositories;
using EventManager.Events.Infrastructure.Authentication;
using EventManager.Events.Infrastructure.Messaging;
using EventManager.Events.Infrastructure.Repositories;
using EventManager.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

namespace EventManager.Events.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
                                                           IConfiguration configuration)
        {
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var postgresPassword = configuration["EVENT_DB_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("EVENT_DB_PASSWORD");

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);

            if (!string.IsNullOrWhiteSpace(postgresPassword))
            {
                connectionStringBuilder.Password = postgresPassword;
            }

            services.AddDbContext<EventDbContext>(options =>
                options.UseNpgsql(connectionStringBuilder.ConnectionString,
                                  npgsqlOptions =>
                                  {
                                      npgsqlOptions.MigrationsAssembly(typeof(EventDbContext).Assembly.FullName);
                                  }
                ));

            services.AddScoped<IEventRepository, EventRepository>();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddHostedService<TopicInitializer>();
            services.AddHostedService<BookingConfirmedConsumer>();

            var jwtSection = configuration.GetSection("Jwt");

            var secret = configuration["JWT_SECRET"]
                ?? throw new InvalidOperationException("JWT Secret не задан");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,//true,
                        //ValidIssuer = jwtSection["Issuer"],

                        ValidateAudience = false, //true,
                        //ValidAudience = jwtSection["Audience"],

                        ValidateLifetime = true,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                    };
                });

            services.AddAuthorization();
          
            return services;
        }

        public static async Task ApplyMigrationsAsync(this IServiceProvider services)
        {
            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EventDbContext>();
                await db.Database.MigrateAsync();
            }
        }
    }
}
