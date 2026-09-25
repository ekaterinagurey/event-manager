using EventManager.Shared.Contracts.Events;

namespace EventManager.Bookings.Application.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishBookingConfirmedAsync(BookingConfirmedEvent @event, CancellationToken cancellationToken = default);
        Task PublishBookingCancelledAsync(BookingCancelledEvent @event, CancellationToken cancellationToken = default);
    }
}
