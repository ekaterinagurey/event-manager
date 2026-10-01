using EventManager.Events.Application.DTOs;
using EventManager.Events.Application.Interfaces;
using EventManager.Events.Application.Mappers;
using EventManager.Events.Domain.Entities;
using EventManager.Events.Domain.Exceptions;
using EventManager.Events.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace EventManager.Events.Application.Services
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ICacheService _cacheService;
        private readonly ILogger<EventService> _logger;

        public EventService(IEventRepository eventRepository,
                            ICacheService cacheService,
                            ILogger<EventService> logger)
        {
            _eventRepository = eventRepository;
            _cacheService = cacheService;
            _logger = logger;
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

        public async Task<EventInfoDTO> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"event:{id}";

            // проверяем в кэше
            var cachedEvent = await _cacheService.GetAsync<EventInfoDTO>(cacheKey, cancellationToken);

            if (cachedEvent != null)
            {
                _logger.LogInformation($"Event {id} взят из кэша");
                return cachedEvent;
            }

            var existingEvent = await _eventRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Событие с id = {id} не найдено.");

            var existingEventDTO = existingEvent.ToResponse();

            // сохраняем в кэш
            await _cacheService.SetAsync(cacheKey, existingEventDTO, TimeSpan.FromMinutes(10), cancellationToken);
            _logger.LogInformation("Event {EventId} получен из БД и сохранён в кэш", id);

            return existingEventDTO;
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
            await _cacheService.RemoveAsync($"event:{id}", cancellationToken);

            return existingEvent.ToResponse();
        }

        public async Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Событие с id = {id} не найдено.");

            await _eventRepository.DeleteAsync(existingEvent, cancellationToken);

            // Инвалидируем кэш после удаления события
             await _cacheService.RemoveAsync($"event:{id}", cancellationToken);

            return true;
        }

        public async Task<bool> DecreaseAvailableSeatsAsync(Guid bookingId,
                                                            Guid eventId,
                                                            int seatsCount,
                                                            CancellationToken cancellationToken = default)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (existingEvent is null)
                return false;

            if (await _eventRepository.TryDecreaseAvailableSeatsAsync(bookingId,
                                                               existingEvent.Id,
                                                               seatsCount,
                                                               cancellationToken) == false)
                return false;

            // Инвалидируем кэш после коммита транзакции
             await _cacheService.RemoveAsync($"event:{eventId}", cancellationToken);
             _logger.LogInformation($"Cache invalidated for event {eventId} after booking {bookingId}");

            return true;
        }

        public async Task<bool> ReleaseSeatsAsync(Guid eventId,
                                                  int seatsCount,
                                                  CancellationToken cancellationToken = default)
        {
            var existingEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (existingEvent is null)
                return false;

            existingEvent.ReleaseSeats(seatsCount);
            await _eventRepository.UpdateAsync(existingEvent, cancellationToken);

            //  Инвалидируем кэш
            await _cacheService.RemoveAsync($"event:{eventId}", cancellationToken);
            _logger.LogInformation($"Cache invalidated for event {eventId} after booking cancellation");

            return true;
        }

        public async Task<List<EventInfoDTO>> GetTopPopularEventsAsync(CancellationToken cancellationToken = default)
        {
            const string cacheKey = "events:top10";

            // проверка кэша
            var cachedTop = await _cacheService.GetAsync<List<EventInfoDTO>>(cacheKey, cancellationToken);

            if (cachedTop != null && cachedTop.Count > 0)

            {
                _logger.LogInformation("Top-10 событий получены из кэша Redis");
                return cachedTop;
            }

            // получаем топ 10 из базы
            var topEvents = await _eventRepository.GetTopEventsAsync(10, cancellationToken);

            var topEventsDTO = topEvents.Select(e => e.ToResponse());

            if (topEvents.Count > 0)
            {
                await _cacheService.SetAsync(cacheKey, topEventsDTO, TimeSpan.FromMinutes(2), cancellationToken);
                _logger.LogInformation("Top-10 событий получены из БД и закэшированы");
            }

            return topEventsDTO.ToList();
        }
    }
}
