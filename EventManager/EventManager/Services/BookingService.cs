using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Models;

namespace EventManager.Services
{
    public class BookingService: IBookingService
    {
        private readonly List<Booking> _bookings = [];

        public Booking CreateBookingAsync(Guid eventId)
        {
            var newBooking = new Booking();
            newBooking.Id = Guid.NewGuid();
            newBooking.EventId = eventId;
            newBooking.Status = BookingStatus.Pending;
            newBooking.CreatedAt = DateTime.Now;

            _bookings.Add(newBooking);
            return newBooking;
        }

        public Booking GetBookingByIdAsync(Guid bookingId)
        {
            return _bookings.FirstOrDefault(x => x.Id == bookingId) ?? 
                throw new NotFoundException($"Бронирование с id '{bookingId}' не найдено.");
            
        }
    }
}
