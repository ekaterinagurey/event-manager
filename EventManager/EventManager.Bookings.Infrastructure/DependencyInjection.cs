using EventManager.Bookings.Application.Interfaces;
using EventManager.Bookings.Domain.Repositories;
using EventManager.Bookings.Infrastructure.Authentication;
using EventManager.Bookings.Infrastructure.Repositories;
using EventManager.Infrastructure.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

namespace EventManager.Bookings.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
                                                           IConfiguration configuration)
        {
            var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var postgresPassword = configuration["BOOKING_DB_PASSWORD"]
                ?? Environment.GetEnvironmentVariable("BOOKING_DB_PASSWORD");

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(rawConnectionString);

            if (!string.IsNullOrWhiteSpace(postgresPassword))
            {
                connectionStringBuilder.Password = postgresPassword;
            }

            services.AddDbContext<BookingDbContext>(options =>
                options.UseNpgsql(connectionStringBuilder.ConnectionString,
                                  npgsqlOptions =>
                                  {
                                      npgsqlOptions.MigrationsAssembly(typeof(BookingDbContext).Assembly.FullName);
                                  }
                ));

            services.AddScoped<IBookingRepository, BookingRepository>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

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
                var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                await db.Database.MigrateAsync();
            }
        }
    }
}
