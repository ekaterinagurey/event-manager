using EventManager.DTOs.Events;
using EventManager.Models;

namespace EventManager.Repositories.Interfaces
{
    public interface IEventRepository
    {
        Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<IEnumerable<Event>> GetPagedAsync(string? title,
                                                DateTime? from,
                                                DateTime? to,
                                                int page = 1,
                                                int pageSize = 10,
                                                CancellationToken cancellationToken = default);
        Task CreateAsync(Event newEvent, CancellationToken cancellationToken);
        Task UpdateAsync(Event newEvent, CancellationToken cancellationToken);
        Task DeleteAsync(Event currentEvent, CancellationToken cancellationToken);
    }
}
