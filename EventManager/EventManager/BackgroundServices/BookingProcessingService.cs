using EventManager.Interfaces;
using EventManager.Models;
using Microsoft.Extensions.Hosting;

namespace EventManager.BackgroundServices
{
    public class BookingProcessingService: BackgroundService
    {
        private readonly IBookingService _bookingService;
        private readonly ILogger<BookingProcessingService> _logger;

        public BookingProcessingService(IBookingService bookingService, ILogger<BookingProcessingService> logger)
        {
            _bookingService = bookingService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingProcessingService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessPendingBookingAsync(stoppingToken);
            }
        }

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
    }
}
