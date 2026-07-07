using EventManager.Models;

namespace EventManager.Interfaces
{
    public interface IBookingService
    {
        Booking CreateBookingAsync(Guid eventId);
        Booking GetBookingByIdAsync(Guid bookingId);
    }
}
