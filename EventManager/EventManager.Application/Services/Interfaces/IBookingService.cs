using EventManager.Domain.Models;

namespace EventManager.Application.Services.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

        Task<IEnumerable<Booking>> GetPendingBookingAsync(CancellationToken cancellationToken = default);
    }
}
