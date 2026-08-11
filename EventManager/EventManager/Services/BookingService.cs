using EventManager.DataAccess;
using EventManager.Exceptions;
using EventManager.Interfaces;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace EventManager.Services
{
    internal sealed class BookingService : IBookingService
    {
        private static readonly SemaphoreSlim BookingLock = new(1, 1);
        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            await BookingLock.WaitAsync(cancellationToken);
            try
            {
                var existEvent = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
                                 ?? throw new NotFoundException("Event not found");

                if (!existEvent.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException("No available seats for this event");
                }

                var booking = Booking.Create(eventId);
                await _context.Bookings.AddAsync(booking, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return booking;
            }
            finally
            {
                BookingLock.Release();
            }
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
                ?? throw new NotFoundException($"Бронирование с id '{bookingId}' не найдено.");

            return booking;
        }

       /* public async Task UpdateBookingAsync(Booking booking, CancellationToken cancellationToken)
        {
            var existingBooking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken)
                                  ?? throw new NotFoundException($"Бронирование с id '{booking.Id}' не найдено.");

            existingBooking.Status = booking.Status;
            existingBooking.ProcessedAt = booking.ProcessedAt;
            return Task.CompletedTask;
        }*/

        /*public async Task<IEnumerable<Booking>> GetPendingBookingAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Bookings
                .Where(b => b.Status == BookingStatus.Pending)
                .ToListAsync(cancellationToken);
        }*/
    }
}
