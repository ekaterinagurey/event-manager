using EventManager.DTOs.Events;
using EventManager.Models;
namespace EventManager.Interfaces
{
    public interface IEventService
    {
        IEnumerable<Event> GetEvents();
        PaginateResultDTO<Event> GetEvents(GetEventsRequestDTO filter);
        Event? GetEvent(Guid id);
        Event AddEvent(Event newEvent);
        bool ChangeEvent(Guid id, Event editingEvent);
        bool RemoveEvent(Guid id);

    }
}
