using EventManager.DataAccess;
using EventManager.Models;
using EventManager.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Repositories
{
    public class BookingRepository: IBookingRepository
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

    }
}
