using EventManager.DTOs;
using EventManager.Models;
namespace EventManager.Interfaces
{
    public interface IEventService
    {
        IEnumerable<Event> GetEvents();
        PaginateResultDTO<Event> GetEvents(GetEventsRequestDTO filter);
        Event? GetEvent(int id);
        Event AddEvent(Event newEvent);
        bool ChangeEvent(int id, Event editingEvent);
        bool RemoveEvent(int id);

    }
}
