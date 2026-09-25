using EventManager.Bookings.Application.Interfaces;
using EventManager.Bookings.Domain.Entities;
using EventManager.Bookings.Domain.Exceptions;
using EventManager.Bookings.Domain.Repositories;
using EventManager.Shared.Contracts.Events;

namespace EventManager.Bookings.Application.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly SemaphoreSlim BookingLock = new(1, 1);
        private readonly IBookingRepository _bookingRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IEventPublisher _eventPublisher;
        private const int MaxActiveBookings = 10;

        public BookingService(IBookingRepository bookingRepository,
                              ICurrentUserService currentUserService,
                              IEventPublisher eventPublisher)
        {
            _bookingRepository = bookingRepository;
            _currentUserService = currentUserService;
            _eventPublisher = eventPublisher;
        }   

        public async Task<Booking> CreateBookingAsync(Guid eventId,
                                                      Guid userId,
                                                      CancellationToken cancellationToken = default)
        {
            await BookingLock.WaitAsync(cancellationToken);
            try
            {
                var activeBookingsCount = await _bookingRepository.CountActiveByUserId(userId, cancellationToken);

                if (activeBookingsCount >= MaxActiveBookings)
                    throw new BookingLimitExceededException();

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
                                             CancellationToken cancellationToken)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken)
            ?? throw new NotFoundException($"Бронирование с id '{bookingId}' не найдено.");

            var currentUserId = _currentUserService.UserId
                ?? throw new UnauthorizedException();

            // Пользователь может отменить, если это его бронь ИЛИ если он админ
            if (booking.UserId != currentUserId && !_currentUserService.IsAdmin)
            {
                throw new AccessDeniedException("У вас нет прав на отмену этой брони.");
            }

            booking.Cancel();

            await _bookingRepository.UpdateAsync(booking, cancellationToken);

            // Публикуем событие для Events Service
            var cancelledEvent = new BookingCancelledEvent(booking.Id,
                                                           booking.EventId,
                                                           booking.UserId,
                                                           1,
                                                           DateTime.UtcNow);

            await _eventPublisher.PublishBookingCancelledAsync(cancelledEvent, cancellationToken);
        }
    }
}
