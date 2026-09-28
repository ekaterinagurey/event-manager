using EventManager.Users.Application.Interfaces;
using EventManager.Users.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Users.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
