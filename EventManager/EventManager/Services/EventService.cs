using EventManager.Interfaces;
using EventManager.Models;
using Microsoft.Extensions.Logging;

namespace EventManager.Services
{
    public class EventService : IEventService
    {
        private readonly List<Event> _events = [];

        public IEnumerable<Event> GetEvents()
        {
            return _events;
        }

        public Event? GetEvent(int id)
        {
            return _events.FirstOrDefault(x => x.Id == id);
        }

        public Event AddEvent(Event newEvent)
        {
            _events.Add(newEvent);
            return newEvent;
        }

        public bool ChangeEvent(int id, Event editingEvent)
        {
            var exitingEvent = GetEvent(id);

            if (exitingEvent == null)
                return false;

            var index = _events.IndexOf(exitingEvent);
            _events[index] = editingEvent;

            return true;
        }

        public bool RemoveEvent(int id)
        {
            var exitingEvent = GetEvent(id);

            if (exitingEvent == null)
                return false;

            _events.Remove(exitingEvent);
            return true;
        }
    }
}
