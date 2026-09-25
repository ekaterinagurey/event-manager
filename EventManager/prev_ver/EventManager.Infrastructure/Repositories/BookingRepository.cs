using EventManager.Infrastructure.DataAccess;
using EventManager.Domain.Models;
using Microsoft.EntityFrameworkCore;
using EventManager.Domain.Enums;
using EventManager.Application.Interfaces.Repositories;

namespace EventManager.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        }

        public async Task<IEnumerable<Booking>> GetPendingAsync(CancellationToken cancellationToken)
        {
            return await _context.Bookings
               .Where(b => b.Status == BookingStatus.Pending)
               .ToListAsync(cancellationToken);
        }

        public async Task CreateAsync(Booking booking, CancellationToken cancellationToken)
        {
            await _context.Bookings.AddAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> CountActiveByUserId(Guid userId, CancellationToken cancellationToken)
        {
            return await _context.Bookings.CountAsync(b => b.UserId == userId &&
                                                    (b.Status == BookingStatus.Pending ||
                                                     b.Status == BookingStatus.Confirmed),
                                               cancellationToken);
        }

    }
}
