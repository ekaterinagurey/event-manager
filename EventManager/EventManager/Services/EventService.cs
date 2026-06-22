using EventManager.Exceptions;
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
            var eventEntity = _events.FirstOrDefault(x => x.Id == id);

            if (eventEntity == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");
            return eventEntity;
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
            {
                throw new NotFoundException($"Событие с id = {id} не найдено.");
            }

            var index = _events.IndexOf(exitingEvent);
            _events[index] = editingEvent;

            return true;
        }

        public bool RemoveEvent(int id)
        {
            var exitingEvent = GetEvent(id);

            if (exitingEvent == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");

            _events.Remove(exitingEvent);
            return true;
        }
    }
}
