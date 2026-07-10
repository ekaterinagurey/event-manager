using EventManager.Models;

namespace EventManager.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId);
        Task<Booking> GetBookingByIdAsync(Guid bookingId);
        Task UpdateBookingAsync(Booking booking);
        Task<IEnumerable<Booking>> GetPendingBookingAsync();
    }
}
