using System;
namespace EventManager.Shared.Contracts.Events
{
    public class BookingCancelledEvent
    {
        public Guid BookingId { get; init; }
        public Guid EventId { get; init; }
        public Guid UserId { get; init; }
        public int SeatsCount { get; init; }
        public DateTime CancelledAtUtc { get; init; }

        public BookingCancelledEvent() { }

        public BookingCancelledEvent(Guid bookingId, 
                                     Guid eventId, 
                                     Guid userId,
                                     int seatsCount,
                                     DateTime cancelledAtUtc)
        {
            BookingId = bookingId;
            EventId = eventId;
            UserId = userId;
            SeatsCount = seatsCount;
            CancelledAtUtc = cancelledAtUtc;
        }
    }
}
