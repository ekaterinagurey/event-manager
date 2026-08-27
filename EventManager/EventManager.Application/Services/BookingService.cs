using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Application.Repositories.Interfaces;
using EventManager.Application.Services.Interfaces;

namespace EventManager.Application.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly SemaphoreSlim BookingLock = new(1, 1);
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;

        public BookingService(IBookingRepository bookingRepository,
                              IEventRepository eventRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            await BookingLock.WaitAsync(cancellationToken);
            try
            {
                var existingEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                                 ?? throw new NotFoundException("Event not found");

                if (!existingEvent.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException("No available seats for this event");
                }

                await _eventRepository.UpdateAsync(existingEvent, cancellationToken);

                var booking = Booking.Create(eventId);
                await _bookingRepository.CreateAsync(booking, cancellationToken);
                return booking;
            }
            finally
            {
                BookingLock.Release();
            }
        }

        public async Task<Booking> GetBookingByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
                ?? throw new NotFoundException($"Бронирование с id '{bookingId}' не найдено.");
            return booking;
        }

        public async Task<IEnumerable<Booking>> GetPendingBookingAsync(CancellationToken cancellationToken = default)
        {
            return await _bookingRepository.GetPendingAsync(cancellationToken);
        }
    }
}
