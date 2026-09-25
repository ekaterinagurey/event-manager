using EventManager.Domain.Models;

namespace EventManager.Application.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task<IEnumerable<Booking>> GetPendingAsync(CancellationToken cancellationToken);
        Task CreateAsync(Booking booking, CancellationToken cancellationToken);
        Task UpdateAsync(Booking booking, CancellationToken cancellationToken);
        Task<int> CountActiveByUserId(Guid userId, CancellationToken cancellationToken);
    }
}
