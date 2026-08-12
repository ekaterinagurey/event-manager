using EventManager.DTOs.Events;
using EventManager.Models;
namespace EventManager.Interfaces
{
    public interface IEventService
    {
        Task<PaginateResultDTO<Event>> GetEventsAsync(GetEventsRequestDTO filter, CancellationToken cancellationToken = default);
        Task<Event> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<EventInfoDTO> CreateEventAsync(CreateEventDTO newEvent, CancellationToken cancellationToken = default);
        Task<EventInfoDTO> UpdateEventAsync(Guid id, UpdateEventDTO editingEvent, CancellationToken cancellationToken = default);
        Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default);

    }
}
