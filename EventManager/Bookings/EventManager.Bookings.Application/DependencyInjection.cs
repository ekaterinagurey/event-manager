using EventManager.Bookings.Application.BackgroundServices;
using EventManager.Bookings.Application.Interfaces;
using EventManager.Bookings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventManager.Bookings.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IBookingService, BookingService>();
            services.AddHostedService<BookingProcessingService>();
            return services;
        }
    }
}
