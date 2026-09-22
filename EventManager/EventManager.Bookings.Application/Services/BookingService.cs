using EventManager.Bookings.Application.Interfaces;
using EventManager.Bookings.Domain.Entities;
using EventManager.Bookings.Domain.Exceptions;
using EventManager.Bookings.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EventManager.Bookings.Application.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly SemaphoreSlim BookingLock = new(1, 1);
        private readonly IBookingRepository _bookingRepository;
        private readonly ICurrentUserService _currentUserService;
      //  private readonly IEventRepository _eventRepository;
        private const int MaxActiveBookings = 10;

        public BookingService(IBookingRepository bookingRepository,
                              ICurrentUserService currentUserService
                             /* IEventRepository eventRepository*/)
        {
            _bookingRepository = bookingRepository;
            _currentUserService = currentUserService;
            // _eventRepository = eventRepository;
        }

        public async Task<Booking> CreateBookingAsync(Guid eventId,
                                                      Guid userId,
                                                      CancellationToken cancellationToken = default)
        {
            await BookingLock.WaitAsync(cancellationToken);
            try
            {
                //var existingEvent = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
                              //   ?? throw new NotFoundException("Event not found");

              //  if (!existingEvent.TryReserveSeats())
               // {
                 //   throw new NoAvailableSeatsException();
              //  }

               // if (existingEvent.HasStarted())
                   // throw new PastEventBookingException();

                var activeBookingsCount = await _bookingRepository.CountActiveByUserId(userId, cancellationToken);

                if (activeBookingsCount >= MaxActiveBookings)
                    throw new BookingLimitExceededException();

              //  await _eventRepository.UpdateAsync(existingEvent, cancellationToken);

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
                                             // Guid userId,
                                             // UserRole userRole,
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

            /*if (userRole != UserRole.Admin &&
                booking.UserId != userId)
                throw new AccessDeniedException();*/

            //var existingEvent = await _eventRepository.GetByIdAsync(booking.EventId, cancellationToken)
                // ?? throw new NotFoundException("Event not found");

           // if (existingEvent.StartAt <= DateTime.UtcNow)
               // throw new EventAlreadyStartedException(existingEvent.Id);

            booking.Cancel();

            await _bookingRepository.UpdateAsync(booking, cancellationToken);
        }
    }
}
