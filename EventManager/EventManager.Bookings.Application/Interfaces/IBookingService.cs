using EventManager.Bookings.Domain.Entities;

namespace EventManager.Bookings.Application.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
        Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> GetPendingBookingAsync(CancellationToken cancellationToken = default);
        Task CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken);
    }
}
