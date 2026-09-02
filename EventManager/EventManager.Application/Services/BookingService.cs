using EventManager.Domain.Exceptions;
using EventManager.Domain.Models;
using EventManager.Application.Services.Interfaces;
using EventManager.Domain.Enums;
using EventManager.Application.Interfaces.Repositories;

namespace EventManager.Application.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly SemaphoreSlim BookingLock = new(1, 1);
        private readonly IBookingRepository _bookingRepository;
        private readonly IEventRepository _eventRepository;
        private const int MaxActiveBookings = 3;

        public BookingService(IBookingRepository bookingRepository,
                              IEventRepository eventRepository)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId,
                                                      Guid userId,
                                                      CancellationToken cancellationToken = default)
        {
            await BookingLock.WaitAsync(cancellationToken);
            try
            {
                var existingEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                                 ?? throw new NotFoundException("Event not found");

                if (!existingEvent.TryReserveSeats())
                {
                    throw new NoAvailableSeatsException();
                }

                if (existingEvent.HasStarted())
                    throw new PastEventBookingException();

                var activeBookingsCount = await _bookingRepository.CountActiveByUserId(userId, cancellationToken);

                if (activeBookingsCount >= MaxActiveBookings)
                    throw new BookingLimitExceededException();

                await _eventRepository.UpdateAsync(existingEvent, cancellationToken);

                var booking = Booking.Create(eventId, userId);
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

        public async Task CancelBookingAsync(Guid bookingId,
                                             Guid userId,
                                             UserRole userRole,
                                             CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронирование с id '{bookingId}' не найдено.");

            if (userRole != UserRole.Admin &&
               booking.UserId != userId)
                throw new AccessDeniedException();

            booking.Cancel();

            await _bookingRepository.UpdateAsync(booking, cancellationToken);
        }
    }
}
