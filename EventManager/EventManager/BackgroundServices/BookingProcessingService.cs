using EventManager.DataAccess;
using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace EventManager.BackgroundServices
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
                    List<Booking> pendingBookings;

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        pendingBookings = await context.Bookings
                            .Where(b => b.Status == BookingStatus.Pending)
                            .ToListAsync(stoppingToken);
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
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
                if (booking == null || booking.Status != BookingStatus.Pending)
                    return;

                var currentEvent = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);
                if (currentEvent == null)
                {
                    booking.Reject();
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogWarning("Booking {BookingId} rejected", booking.Id);
                    return;
                }

                booking.Confirm();
                await context.SaveChangesAsync(stoppingToken);

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
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var booking = await context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, stoppingToken);
                    if (booking != null)
                    {
                        booking.Reject();

                        var currentEvent = await context.Events.FirstOrDefaultAsync(e => e.Id == booking.EventId, stoppingToken);
                        if (currentEvent != null)
                            currentEvent.ReleaseSeats();

                        await context.SaveChangesAsync(stoppingToken);
                        _logger.LogError(ex, $"Booking {bookingId} rejected due to processing error");
                    }
                }
                catch (Exception exExt)
                {
                    _logger.LogError(exExt,$"Failed to reject booking {bookingId} after error");
                }
            }
        }
    }
}
