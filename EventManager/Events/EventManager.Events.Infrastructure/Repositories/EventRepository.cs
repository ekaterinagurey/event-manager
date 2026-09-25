using EventManager.Events.Domain.Entities;
using EventManager.Events.Domain.Repositories;
using EventManager.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace EventManager.Events.Infrastructure.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly EventDbContext _context;

        public EventRepository(EventDbContext context)
        {
            _context = context;
        }
        public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<(List<Event> Events, int TotalCount)> GetPagedAsync(string? title,
                                                                              DateTime? from,
                                                                              DateTime? to,
                                                                              int page = 1,
                                                                              int pageSize = 10,
                                                                              CancellationToken cancellationToken = default)
        {
            IQueryable<Event> query = _context.Events;

            if (!string.IsNullOrWhiteSpace(title))
            {
                var titleLower = title.ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(titleLower));
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.StartAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.EndAt <= to.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var events = await query
                .OrderBy(x => x.StartAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (events, totalCount);
        }

        public async Task CreateAsync(Event newEvent, CancellationToken cancellationToken)
        {
            await _context.Events.AddAsync(newEvent, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Event newEvent, CancellationToken cancellationToken)
        {
            _context.Events.Update(newEvent);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Event currentEvent, CancellationToken cancellationToken)
        {
            _context.Events.Remove(currentEvent);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
