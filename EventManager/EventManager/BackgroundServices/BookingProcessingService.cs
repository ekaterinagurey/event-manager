using EventManager.Interfaces;
using EventManager.Models;
using Microsoft.Extensions.Hosting;

namespace EventManager.BackgroundServices
{
    public class BookingProcessingService : BackgroundService
    {
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;
        private readonly ILogger<BookingProcessingService> _logger;
        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        public BookingProcessingService(IBookingService bookingService,
                                        IEventService eventService,
                                        ILogger<BookingProcessingService> logger)
        {
            _bookingService = bookingService;
            _eventService = eventService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingProcessingService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                //await ProcessPendingBookingAsync(stoppingToken);
                var pendingBookings = _bookingService.GetPendingBookingAsync().Result.ToList();
                var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                await Task.WhenAll(tasks);
                await Task.Delay(1000, stoppingToken);
            }
        }

        public async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(2000, stoppingToken);
                await _processingSemaphore.WaitAsync(stoppingToken);

                try
                {
                    var currentEvent = _eventService.GetEvent(booking.EventId);

                    if (currentEvent == null)
                    {
                        booking.Reject();

                        await _bookingService.UpdateBookingAsync(booking);
                        _logger.LogWarning("Booking {BookingId} rejected", booking.Id);
                        return;
                    }

                    booking.Confirm();

                    await _bookingService.UpdateBookingAsync(booking);
                    _logger.LogInformation("Booking {BookingId} confirmed.", booking.Id);
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing booking {BookingId}",booking.Id);
                await _processingSemaphore.WaitAsync(stoppingToken);

                try
                {
                    booking.Reject();
                    var currentEvent = _eventService.GetEvent(booking.EventId);

                    if (currentEvent != null)
                    {
                        currentEvent.ReleaseSeats();
                        _eventService.ChangeEvent(currentEvent.Id, currentEvent);
                    }

                    await _bookingService.UpdateBookingAsync(booking);
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
        }

        /*
         public async Task ProcessPendingBookingAsync(CancellationToken stoppingToken)
        {
            var pendingBooking = await _bookingService.GetPendingBookingAsync();

            foreach (var booking in pendingBooking)
            {
                _logger.LogInformation("Processing booking {BookingId}.", booking.Id);

                await Task.Delay(2000, stoppingToken);
                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.Now;
                await _bookingService.UpdateBookingAsync(booking);

                _logger.LogInformation("Booking {BookingId} confirmed.", booking.Id);
            }
        }
        */
    }
}
