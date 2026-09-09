using EventManager.Domain.Enums;
using EventManager.Domain.Models;

namespace EventManager.Application.Services.Interfaces
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
        Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Booking>> GetPendingBookingAsync(CancellationToken cancellationToken = default);
        Task CancelBookingAsync(Guid bookingId, Guid userId, UserRole userRole, CancellationToken cancellationToken);
    }
}
