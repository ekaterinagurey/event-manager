using EventManager.DTOs.Events;
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

        public Event? GetEvent(Guid id)
        {
            var eventEntity = _events.FirstOrDefault(x => x.Id == id);

            if (eventEntity == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");
            return eventEntity;
        }

        public Task<EventInfoDTO> CreateEventAsync(CreateEventDTO newEvent)
        {
            if (string.IsNullOrWhiteSpace(newEvent.Title))
                throw new ArgumentException("Заголовок события обязателен для заполнения.");

            if (newEvent.EndAt <= newEvent.StartAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            var createdEvent = Event.Create(newEvent.Title,
                                            newEvent.Description,
                                            newEvent.StartAt,
                                            newEvent.EndAt,
                                            newEvent.TotalSeats);

            _events.Add(createdEvent);
            return Task.FromResult(createdEvent.ToResponse());
        }

        public bool ChangeEvent(Guid id, Event editingEvent)
        {
            var exitingEvent = GetEvent(id);
            
            if (exitingEvent == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");

            if (string.IsNullOrWhiteSpace(editingEvent.Title))
                throw new ArgumentException("Заголовок события обязателен для заполнения.");

            if (editingEvent.EndAt <= editingEvent.StartAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            editingEvent.Id = id;
            editingEvent.AvailableSeats = exitingEvent.AvailableSeats;

            var index = _events.IndexOf(exitingEvent);
            _events[index] = editingEvent;
            return true;
        }

        public bool RemoveEvent(Guid id)
        {
            var exitingEvent = GetEvent(id);

            if (exitingEvent == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");

            _events.Remove(exitingEvent);
            return true;
        }
    }
}
