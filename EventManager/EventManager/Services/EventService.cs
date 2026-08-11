using EventManager.DataAccess;
using EventManager.DTOs.Events;
using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Mappers;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EventManager.Services
{
    internal sealed class EventService : IEventService
    {
        private readonly AppDbContext _context;
        public EventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaginateResultDTO<Event>> GetEventsAsync(GetEventsRequestDTO filter, CancellationToken cancellationToken = default)
        {
            var query = _context.Events.AsQueryable();

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

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new PaginateResultDTO<Event>
            {
                TotalCount = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = items
            };
        }

        public async Task<Event> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var eventEntity = await _context.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new NotFoundException($"Событие с id = {id} не найдено.");
            return eventEntity;
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

            await _context.Events.AddAsync(createdEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return createdEvent.ToResponse();
        }

        public async Task<EventInfoDTO> UpdateEventAsync(Guid id, Event editingEvent, CancellationToken cancellationToken = default)
        {
            var exitingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Событие с id = {id} не найдено.");

            exitingEvent.Update(editingEvent.Title, editingEvent.StartAt, editingEvent.EndAt, editingEvent.TotalSeats, editingEvent.Description);
            await _context.SaveChangesAsync(cancellationToken);
            return exitingEvent.ToResponse();
        }

        public async Task<bool> RemoveEventAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var exitingEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            if (exitingEvent == null)
                throw new NotFoundException($"Событие с id = {id} не найдено.");

            _context.Events.Remove(exitingEvent);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
