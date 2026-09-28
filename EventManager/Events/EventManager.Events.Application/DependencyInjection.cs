using EventManager.Events.Application.Interfaces;
using EventManager.Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Events.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            return services;
        }
    }
}
