using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Application.Repositories.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EventManager.Domain.Enums;

namespace EventManager.Application.BackgroundServices
{
    public class BookingProcessingService : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingProcessingService> _logger;

        public BookingProcessingService(IServiceScopeFactory scopeFactory,
                                        ILogger<BookingProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("BookingProcessingService started.");
                    IEnumerable<Booking> pendingBookings;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                        pendingBookings = await bookingRepository.GetPendingAsync(stoppingToken);
                    }

                    var tasks = pendingBookings.Select(b =>
                    ProcessBookingAsync(b.Id, stoppingToken));

                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing pending bookings");
                }

                await Task.Delay(PollingInterval, stoppingToken);
            }
        }

        public async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(ProcessingDelay, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return;

                var currentEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

                if (currentEvent == null)
                {
                    booking.Reject();
                    await bookingRepository.UpdateAsync(booking, stoppingToken);

                    _logger.LogWarning("Booking {BookingId} rejected", booking.Id);
                    return;
                }

                booking.Confirm();
                await bookingRepository.UpdateAsync(booking, stoppingToken);

                _logger.LogInformation($"Booking {booking.Id} confirmed.");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while processing booking {bookingId}");
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                    var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                    var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);

                    if (booking != null)
                    {
                        booking.Reject();
                        await bookingRepository.UpdateAsync(booking, stoppingToken);

                        var currentEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

                        if (currentEvent != null)
                        {
                            currentEvent.ReleaseSeats();
                            await eventRepository.UpdateAsync(currentEvent, stoppingToken);
                        }

                        _logger.LogError(ex, $"Booking {bookingId} rejected due to processing error");
                    }
                }
                catch (Exception exExt)
                {
                    _logger.LogError(exExt, $"Failed to reject booking {bookingId} after error");
                }
            }
        }
    }
}
