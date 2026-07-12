using EventManager.Interfaces;
using EventManager.Models;
using Microsoft.Extensions.Hosting;

namespace EventManager.BackgroudServices
{
    public class BookingProcessingService: BackgroundService
    {
        private readonly IBookingService _bookingService;

        public BookingProcessingService(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Выполняем полезную работу
                var pendingBooking = await _bookingService.GetPendingBookingAsync();

                foreach (var booking in pendingBooking)
                {
                    booking.Status = BookingStatus.Confirmed;
                    booking.ProcessedAt = DateTime.Now;
                    await _bookingService.UpdateBookingAsync(booking);
                }
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
