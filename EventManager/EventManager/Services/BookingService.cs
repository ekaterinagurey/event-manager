using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Models;

namespace EventManager.Services
{
    public class BookingService: IBookingService
    {
        private readonly List<Booking> _bookings = [];

        public async Task<Booking> CreateBookingAsync(Guid eventId)
        {
            var newBooking = new Booking();
            newBooking.Id = Guid.NewGuid();
            newBooking.EventId = eventId;
            newBooking.Status = BookingStatus.Pending;
            newBooking.CreatedAt = DateTime.Now;
            _bookings.Add(newBooking);
            return await Task.FromResult(newBooking); ;
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId)
        {
            var booking = _bookings.FirstOrDefault(x => x.Id == bookingId);

            if(booking is null)
                throw new NotFoundException($"Бронирование с id '{bookingId}' не найдено.");

            return await Task.FromResult(booking); 
        }

        public Task UpdateBookingAsync(Booking booking)
        {
            var existingBooking = _bookings.FirstOrDefault(x => x.Id == booking.Id)
                ?? throw new NotFoundException($"Бронирование с id '{booking.Id}' не найдено.");

            existingBooking.Status = booking.Status;
            existingBooking.ProcessedAt = booking.ProcessedAt;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Booking>> GetPendingBookingAsync()
        {
            var pendingBookings = _bookings.Where(x => x.Status == BookingStatus.Pending);
            return Task.FromResult(pendingBookings);
        }
    }
}
