using EventManager.Domain.Models;

namespace EventManager.Application.Interfaces.Repositories
{
    public interface IEventRepository
    {
        Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task<(List<Event> Events, int TotalCount)> GetPagedAsync(string? title,
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
