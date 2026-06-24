using EventManager.DTOs;
using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Mappers;
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
        public PaginateResultDTO<Event> GetEvents(GetEventsRequestDTO filter)
        {
            var query = _events.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                query = query.Where(x => x.Title.Contains(filter.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.From.HasValue)
            {
                query = query.Where(x => x.StartAt >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(x => x.EndAt <= filter.To.Value);
            }

            var totalItems = query.Count();

            var items = query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new PaginateResultDTO<Event>
            {
                TotalCount = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = items
            };
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
            if (newEvent.Id <= 0)
                throw new ArgumentException("Идентификатор события обязателен для заполнения.");

            if (string.IsNullOrWhiteSpace(newEvent.Title))
                throw new ArgumentException("Заголовок события обязателен для заполнения.");

            if (newEvent.EndAt <= newEvent.StartAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            _events.Add(newEvent);
            return newEvent;
        }

        public bool ChangeEvent(int id, Event editingEvent)
        {
            var exitingEvent = GetEvent(id);

            if (exitingEvent == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");

            if (exitingEvent.Id <= 0)
                throw new ArgumentException("Идентификатор события обязателен для заполнения.");

            if (string.IsNullOrWhiteSpace(editingEvent.Title))
                throw new ArgumentException("Заголовок события обязателен для заполнения.");

            if (editingEvent.EndAt <= editingEvent.StartAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

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
