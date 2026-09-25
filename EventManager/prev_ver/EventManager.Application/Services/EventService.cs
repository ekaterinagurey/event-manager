using EventManager.Application.DTOs.Events;
using EventManager.Domain.Exceptions;
using EventManager.Application.Mappers;
using EventManager.Domain.Models;
using EventManager.Application.Services.Interfaces;
using EventManager.Application.Interfaces.Repositories;

namespace EventManager.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<PaginateResultDTO<Event>> GetEventsAsync(GetEventsRequestDTO filter, CancellationToken cancellationToken = default)
        {

            var events = await _eventRepository.GetPagedAsync(filter.Title,
                                                              filter.From,
                                                              filter.To,
                                                              filter.Page,
                                                              filter.PageSize,
                                                              cancellationToken);

            return new PaginateResultDTO<Event>
            {
                TotalCount = events.TotalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = events.Events
            };
        }

        public async Task<Event> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException($"Событие с id = {id} не найдено.");
            return existingEvent;
        }

        public async Task<EventInfoDTO> CreateEventAsync(CreateEventDTO newEvent, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(newEvent.Title))
                throw new ArgumentException("Заголовок события обязателен для заполнения.");

            if (newEvent.EndAt <= newEvent.StartAt)
                throw new ArgumentException("EndAt должна быть позже StartAt.");

            var createdEvent = Event.Create(newEvent.Title,
                                            newEvent.StartAt,
                                            newEvent.EndAt,
                                            newEvent.TotalSeats,
                                            newEvent.Description);

            await _eventRepository.CreateAsync(createdEvent, cancellationToken);
            return createdEvent.ToResponse();
        }

        public async Task<EventInfoDTO> UpdateEventAsync(Guid id, UpdateEventDTO editingEvent, CancellationToken cancellationToken = default)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Событие с id = {id} не найдено.");

            existingEvent.Update(editingEvent.Title,
                                 editingEvent.StartAt,
                                 editingEvent.EndAt,
                                 editingEvent.Description);

            await _eventRepository.UpdateAsync(existingEvent, cancellationToken);
            return existingEvent.ToResponse();
        }

        public async Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Событие с id = {id} не найдено.");

            await _eventRepository.DeleteAsync(existingEvent, cancellationToken);
            return true;
        }
    }
}
