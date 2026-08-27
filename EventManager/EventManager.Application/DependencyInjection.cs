using EventManager.Application.BackgroundServices;
using EventManager.Application.Services;
using EventManager.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEventService, EventService>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddHostedService<BookingProcessingService>();
            return services;
        }
    }
}
