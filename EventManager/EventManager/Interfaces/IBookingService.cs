using EventManager.Models;

namespace EventManager.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    }
}
