using EventManager.Bookings.Application.Interfaces;
using EventManager.Bookings.Domain.Entities;
using EventManager.Bookings.Domain.Enums;
using EventManager.Bookings.Domain.Repositories;
using EventManager.Shared.Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace EventManager.Bookings.Application.BackgroundServices
{
    public class BookingProcessingService : BackgroundService
    {
        private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<BookingProcessingService> _logger;

        public BookingProcessingService(IServiceScopeFactory scopeFactory,
                                        IEventPublisher eventPublisher,
                                        ILogger<BookingProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _eventPublisher = eventPublisher;
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

                var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return;

                /* var currentEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

                 if (currentEvent == null)
                 {
                     booking.Reject();
                     await bookingRepository.UpdateAsync(booking, stoppingToken);

                     _logger.LogWarning("Booking {BookingId} rejected", booking.Id);
                     return;
                 }*/

                booking.Confirm();
                await bookingRepository.UpdateAsync(booking, stoppingToken);

                _logger.LogInformation($"Booking {booking.Id} confirmed.");

                // Публикация события о бронировании для сервиса Event
                var confirmedEvent = new BookingConfirmedEvent(booking.Id,
                                                               booking.EventId,
                                                               booking.UserId,
                                                               1,
                                                               DateTime.UtcNow);

                await _eventPublisher.PublishBookingConfirmedAsync(confirmedEvent, stoppingToken);
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

                    var booking = await bookingRepository.GetByIdAsync(bookingId, stoppingToken);

                    if (booking != null)
                    {
                        booking.Reject();
                        await bookingRepository.UpdateAsync(booking, stoppingToken);

                        /* var currentEvent = await eventRepository.GetByIdAsync(booking.EventId, stoppingToken);

                         if (currentEvent != null)
                         {
                             currentEvent.ReleaseSeats();
                             await eventRepository.UpdateAsync(currentEvent, stoppingToken);
                         }*/

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
